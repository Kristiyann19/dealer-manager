using DealerManager.Application.Dtos.Candidate;
using DealerManager.Application.Dtos.Dashboard;
using DealerManager.Application.IService.Candidate;
using DealerManager.Application.IService.Dashboard;
using DealerManager.Domain.Enums;
using DealerManager.Infrastructure.Persistence;
using DealerManager.Infrastructure.Service.Finance;
using Microsoft.EntityFrameworkCore;

namespace DealerManager.Infrastructure.Service.Dashboard;

public sealed class DashboardService(DealerManagerDbContext db, TimeProvider clock,
    ICandidateFinancialCalculator calculator) : IDashboardService
{
    public async Task<DashboardDto> GetDashboard(CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var vehicles = db.Vehicles.AsNoTracking();
        var active = vehicles.Where(v => v.Status != VehicleStatus.Sold);
        var movements = db.FinancialTransactions.AsNoTracking();
        var investments = FinancialQueries.Investments(vehicles, movements);
        var activeCandidates = db.Candidates.AsNoTracking().Where(c => c.Status != CandidateStatus.Purchased && c.Status != CandidateStatus.Rejected);
        var counts = await active.GroupBy(v => v.Status).Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);
        var candidateCount = await activeCandidates.CountAsync(cancellationToken);
        var cash = await FinancialQueries.AccountBalances(db.CapitalAccounts.AsNoTracking()
                .Where(a => a.IsActive && a.Currency.Trim().ToUpper() == "EUR"), movements)
            .SumAsync(a => (decimal?)a.CurrentBalance, cancellationToken) ?? 0m;
        var invested = await FinancialQueries.Investments(active, movements)
            .SumAsync(v => (decimal?)v.TotalInvested, cancellationToken) ?? 0m;
        var remaining = await FinancialQueries.RemainingCosts(db.VehicleCostPlanItems.AsNoTracking()
                .Where(p => p.Vehicle.Status != VehicleStatus.Sold))
            .SumAsync(cancellationToken);

        // UTC calendar days (not rounded elapsed hours); project scalars, never navigation graphs.
        var today = now.UtcDateTime.Date;
        var cutoff = new DateTimeOffset(today.AddDays(-30), TimeSpan.Zero);
        var agedRows = await (from vehicle in active.Where(v => v.PurchaseDate < cutoff)
            join investment in investments on vehicle.Id equals investment.VehicleId
            select new {
                vehicle.Id, vehicle.Make, vehicle.Model, vehicle.Status, vehicle.PurchaseDate,
                investment.TotalInvested,
                ListedAt = vehicle.Status == VehicleStatus.Listed
                    ? db.VehicleListings.Where(l => l.VehicleId == vehicle.Id && l.IsActive)
                        .OrderByDescending(l => l.Id).Select(l => (DateTimeOffset?)l.ListedAt).FirstOrDefault() : null,
                ListingPrice = db.VehicleListings.Where(l => l.VehicleId == vehicle.Id && l.IsActive)
                    .OrderByDescending(l => l.Id).Select(l => (decimal?)l.ListingPrice).FirstOrDefault()
            }).ToListAsync(cancellationToken);
        var agedVehicles = agedRows.Select(v => new AgedVehicleDto {
            Id = v.Id, Make = v.Make, Model = v.Model, Status = v.Status,
            DaysInStock = (today - v.PurchaseDate.UtcDateTime.Date).Days,
            DaysListed = v.ListedAt.HasValue ? Math.Max(0, (today - v.ListedAt.Value.UtcDateTime.Date).Days) : null,
            TotalInvested = v.TotalInvested, ListingPrice = v.ListingPrice
        }).OrderByDescending(v => v.DaysInStock).ThenByDescending(v => v.TotalInvested)
            .ThenBy(v => v.Id).Take(5).ToList();

        // Only five rows are materialized; latest estimate sums are computed by SQL.
        var recent = await activeCandidates.OrderByDescending(c => c.CreatedAt).ThenByDescending(c => c.Id)
            .Take(5).Select(c => new {
                c.Id, c.Make, c.Model, c.Year, c.Mileage, c.Status, c.CreatedAt,
                ExpectedSellingPrice = c.CandidateEstimates.OrderByDescending(e => e.Version).ThenByDescending(e => e.Id)
                    .Select(e => (decimal?)e.ExpectedSellingPrice).FirstOrDefault(),
                EstimatedTotal = c.CandidateEstimates.OrderByDescending(e => e.Version).ThenByDescending(e => e.Id)
                    .Select(e => e.CandidateEstimateItems.Sum(i => (decimal?)i.EstimatedAmount) ?? 0m).Select(x => (decimal?)x).FirstOrDefault()
            }).ToListAsync(cancellationToken);
        var candidates = recent.Select(c => {
            var analysis = c.ExpectedSellingPrice is { } price ? calculator.Calculate(price, [c.EstimatedTotal ?? 0m]) : null;
            return new CandidateListDto { Id = c.Id, Make = c.Make, Model = c.Model, Year = c.Year, Mileage = c.Mileage,
                Status = c.Status, CreatedAt = c.CreatedAt, ExpectedSellingPrice = c.ExpectedSellingPrice,
                EstimatedTotalCost = analysis?.EstimatedTotalCost, ExpectedProfit = analysis?.ExpectedProfit, ExpectedRoi = analysis?.ExpectedRoi };
        }).ToList();

        var latest = await active.OrderByDescending(v => v.CreatedAt).ThenByDescending(v => v.Id).Take(5)
            .Select(v => new { v.Id, v.Make, v.Model, v.Year, v.Status,
                ExpectedSellingPrice = v.SourceCandidate == null ? null : v.SourceCandidate.CandidateEstimates
                    .Where(e => e.IsDecisionSnapshot).Select(e => (decimal?)e.ExpectedSellingPrice).FirstOrDefault(),
                ListingPrice = v.VehicleListings.Where(l => l.IsActive).OrderByDescending(l => l.Id)
                    .Select(l => (decimal?)l.ListingPrice).FirstOrDefault()
            }).ToListAsync(cancellationToken);
        var ids = latest.Select(v => v.Id).ToArray();
        // Bounded batch queries for the active preview; never GetDetails() in a loop.
        var latestInvestments = await investments.Where(v => ids.Contains(v.VehicleId))
            .ToDictionaryAsync(v => v.VehicleId, v => v.TotalInvested, cancellationToken);
        var latestPlans = await db.VehicleCostPlanItems.AsNoTracking().Where(p => ids.Contains(p.VehicleId))
            .Select(p => new { p.VehicleId, Plan = new DealerManager.Application.Dtos.Vehicle.VehicleCostPlanItemDto {
                CurrentEstimatedAmount = p.CurrentEstimatedAmount, CommittedAmount = p.CommittedAmount,
                IsCancelled = p.IsCancelled, ActualPaid = p.VehicleExpenses.Where(e => !e.IsReversed).Sum(e => (decimal?)e.Amount) ?? 0m
            } }).ToListAsync(cancellationToken);
        var activeRows = latest.Select(v => new DashboardVehicleDto {
            Id = v.Id, Make = v.Make, Model = v.Model, Year = v.Year, Status = v.Status,
            TotalInvested = latestInvestments.GetValueOrDefault(v.Id),
            RemainingProjectedCosts = latestPlans.Where(p => p.VehicleId == v.Id).Sum(p => p.Plan.RemainingProjected),
            ExpectedSellingPrice = v.ExpectedSellingPrice, ListingPrice = v.ListingPrice
        }).ToList();

        var monthly = new List<MonthlyFinancialDto>();
        // Six fixed calendar buckets also work with providers that cannot translate DateTimeOffset.Month.
        for (var offset = -5; offset <= 0; offset++)
        {
            var start = monthStart.AddMonths(offset);
            var end = start.AddMonths(1);
            var spending = await movements.Where(t => t.OccurredAt >= start && t.OccurredAt < end
                    && (t.Type == TransactionType.VehiclePurchase || t.Type == TransactionType.VehicleExpense)
                    && t.CapitalAccount.Currency.Trim().ToUpper() == "EUR")
                .SumAsync(t => (decimal?)(t.Direction == TransactionDirection.Out ? t.Amount : -t.Amount), cancellationToken) ?? 0m;
            var sold = await (from sale in db.VehicleSales.AsNoTracking()
                              join investment in investments on sale.VehicleId equals investment.VehicleId
                              where sale.SoldAt >= start && sale.SoldAt < end
                              select new { sale.SalePrice, investment.TotalInvested })
                .GroupBy(_ => 1).Select(g => new { Count = g.Count(), Revenue = g.Sum(s => s.SalePrice),
                    Profit = g.Sum(s => s.SalePrice - s.TotalInvested) }).SingleOrDefaultAsync(cancellationToken);
            monthly.Add(new MonthlyFinancialDto { Month = start, CapitalInvested = spending,
                SalesCount = sold?.Count ?? 0, SalesRevenue = sold?.Revenue ?? 0m, RealizedProfit = sold?.Profit ?? 0m });
        }
        var attention = await DashboardAttentionQueries.Read(db, now, cancellationToken);
        return new DashboardDto {
            AttentionCount = attention.Count, AttentionItems = attention.Take(5).ToList(),
            AsOf = now,
            AgedVehicles = agedVehicles,
            Monthly = new() { CarsInStock = counts.Values.Sum(), CarsInRepair = counts.GetValueOrDefault(VehicleStatus.Repairing),
                SoldThisMonth = monthly[^1].SalesCount, ProfitThisMonth = monthly[^1].RealizedProfit },
            Financial = new() { AvailableCash = cash, CapitalInvested = invested, UpcomingProjectedCosts = remaining },
            Pipeline = new() { Candidates = candidateCount, Transporting = counts.GetValueOrDefault(VehicleStatus.Transporting),
                Repairing = counts.GetValueOrDefault(VehicleStatus.Repairing), ReadyForSale = counts.GetValueOrDefault(VehicleStatus.ReadyForSale),
                Listed = counts.GetValueOrDefault(VehicleStatus.Listed) },
            Candidates = candidates, ActiveVehicles = activeRows, MonthlyFinancials = monthly
        };
    }
}
