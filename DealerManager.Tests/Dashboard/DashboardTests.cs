using System.Net.Http.Json;
using DealerManager.Application.Dtos.Dashboard;
using DealerManager.Domain.Entities;
using DealerManager.Domain.Enums;
using DealerManager.Infrastructure.Persistence;
using DealerManager.Tests.Finance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
namespace DealerManager.Tests.Dashboard;

public class DashboardTests
{
    [Fact]
    public async Task EmptyDashboardReturnsSixZeroMonthsAndBoundedReadQueries()
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient();
        factory.Queries.Commands.Clear();
        var data = (await client.GetFromJsonAsync<DashboardDto>("/api/dashboard"))!;
        Assert.Equal(0, data.Monthly.CarsInStock); Assert.Equal(0, data.Monthly.SoldThisMonth);
        Assert.Equal(0, data.Financial.NetWorthAtCost); Assert.Empty(data.Candidates); Assert.Empty(data.ActiveVehicles);
        Assert.Equal(6, data.MonthlyFinancials.Count);
        Assert.Equal(new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.Zero), data.MonthlyFinancials[0].Month);
        Assert.All(data.MonthlyFinancials, m => { Assert.Equal(0, m.CapitalInvested); Assert.Equal(0, m.SalesRevenue); Assert.Equal(0, m.RealizedProfit); });
        Assert.InRange(factory.Queries.Commands.Count, 19, 21);
        Assert.All(factory.Queries.Commands, q => Assert.StartsWith("SELECT", q.TrimStart()));
    }

    [Fact]
    public async Task AggregatesRealMoneyStatusesLatestEstimatesAndCalendarBoundaries()
    {
        using var factory = new FinanceApiFactory(); using var client = factory.CreateInitializedClient();
        using var scope = factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<DealerManagerDbContext>();
        var now = factory.Clock.Now;
        var account = new CapitalAccount { Name = "EUR", Currency = "EUR", IsActive = true };
        var other = new CapitalAccount { Name = "USD", Currency = "USD", IsActive = true };
        var inactive = new CapitalAccount { Name = "Closed", Currency = "EUR", IsActive = false };
        var second = new CapitalAccount { Name = "Second", Currency = "eur", IsActive = true };
        db.AddRange(account, other, inactive, second); await db.SaveChangesAsync();
        void Movement(int accountId, decimal amount, TransactionDirection direction, TransactionType type, DateTimeOffset date, int? vehicleId = null)
            => db.FinancialTransactions.Add(new() { CapitalAccountId = accountId, Amount = amount, Direction = direction, Type = type, OccurredAt = date, CreatedAt = now, VehicleId = vehicleId });
        Movement(account.Id, 20000, TransactionDirection.In, TransactionType.CapitalContribution, now);
        Movement(other.Id, 100000, TransactionDirection.In, TransactionType.CapitalContribution, now);
        Movement(inactive.Id, 100000, TransactionDirection.In, TransactionType.CapitalContribution, now);
        Movement(second.Id, 500, TransactionDirection.In, TransactionType.CapitalContribution, now);
        var vehicles = Enum.GetValues<VehicleStatus>().Select((status, i) => new DealerManager.Domain.Entities.Vehicle {
            Make = "BMW", Model = status.ToString(), Year = 2020, Status = status, CreatedAt = now.AddDays(i), PurchaseDate = now
        }).ToArray();
        db.Vehicles.AddRange(vehicles); await db.SaveChangesAsync();
        var repair = vehicles.Single(v => v.Status == VehicleStatus.Repairing);
        Movement(account.Id, 6000, TransactionDirection.Out, TransactionType.VehiclePurchase, now, repair.Id);
        var plan = new VehicleCostPlanItem { VehicleId = repair.Id, CurrentEstimatedAmount = 2000, CommittedAmount = 1500 };
        db.Add(plan); await db.SaveChangesAsync();
        var paid = new FinancialTransaction { CapitalAccountId = account.Id, VehicleId = repair.Id, Amount = 1000,
            Direction = TransactionDirection.Out, Type = TransactionType.VehicleExpense, OccurredAt = now, CreatedAt = now };
        db.Add(paid); await db.SaveChangesAsync();
        db.VehicleExpenses.Add(new() { VehicleId = repair.Id, CostPlanItemId = plan.Id, Amount = 1000, FinancialTransactionId = paid.Id, PaidAt = now });
        db.VehicleCostPlanItems.Add(new() { VehicleId = repair.Id, CurrentEstimatedAmount = 9999, IsCancelled = true });
        await db.SaveChangesAsync();
        async Task Sale(decimal price, decimal cost, DateTimeOffset date)
        {
            var v = new DealerManager.Domain.Entities.Vehicle { Make = "Sold", Model = "Car", Status = VehicleStatus.Sold, CreatedAt = now };
            db.Add(v); await db.SaveChangesAsync();
            Movement(account.Id, cost, TransactionDirection.Out, TransactionType.VehiclePurchase, date, v.Id);
            var t = new FinancialTransaction { CapitalAccountId = account.Id, VehicleId = v.Id, Type = TransactionType.VehicleSale,
                Direction = TransactionDirection.In, Amount = price, OccurredAt = date, CreatedAt = now };
            db.Add(t); await db.SaveChangesAsync();
            db.VehicleSales.Add(new() { VehicleId = v.Id, SalePrice = price, SoldAt = date, FinancialTransactionId = t.Id });
            db.VehicleCostPlanItems.Add(new() { VehicleId = v.Id, CurrentEstimatedAmount = 50000 });
            await db.SaveChangesAsync();
        }
        var start = new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero);
        await Sale(6800, 6469, start); await Sale(7500, 8000, start.AddMonths(1).AddTicks(-1));
        await Sale(9000, 7000, start.AddTicks(-1)); await Sale(9000, 7000, start.AddMonths(1));
        for (var i = 0; i < 8; i++) db.Candidates.Add(new() { Make = "Candidate", Model = i.ToString(), CreatedAt = now.AddMinutes(i), Status = i == 7 ? CandidateStatus.Rejected : CandidateStatus.UnderReview,
            CandidateEstimates = [new() { Version = 1, ExpectedSellingPrice = 99999 }, new() { Version = 2, ExpectedSellingPrice = 7000,
                CandidateEstimateItems = [new() { EstimatedAmount = 5000 }] }] });
        db.Candidates.Add(new() { Make = "Purchased", Status = CandidateStatus.Purchased, CreatedAt = now.AddDays(1) });
        await db.SaveChangesAsync(); factory.Queries.Commands.Clear();
        var data = (await client.GetFromJsonAsync<DashboardDto>("/api/dashboard"))!;
        Assert.Equal(9, data.Monthly.CarsInStock); Assert.Equal(1, data.Monthly.CarsInRepair);
        Assert.Equal(2, data.Monthly.SoldThisMonth); Assert.Equal(-169, data.Monthly.ProfitThisMonth);
        Assert.Equal(17331, data.Financial.AvailableCash); Assert.Equal(7000, data.Financial.CapitalInvested);
        Assert.Equal(500, data.Financial.UpcomingProjectedCosts); Assert.Equal(24331, data.Financial.NetWorthAtCost);
        Assert.Equal(7, data.Pipeline.Candidates); Assert.Equal(1, data.Pipeline.Transporting); Assert.Equal(1, data.Pipeline.Repairing);
        Assert.Equal(1, data.Pipeline.ReadyForSale); Assert.Equal(1, data.Pipeline.Listed);
        Assert.Equal(new[] { "6", "5", "4", "3", "2" }, data.Candidates.Select(c => c.Model));
        Assert.All(data.Candidates, c => { Assert.Equal(5000, c.EstimatedTotalCost); Assert.Equal(2000, c.ExpectedProfit); Assert.Equal(40, c.ExpectedRoi); });
        Assert.Equal(5, data.ActiveVehicles.Count); Assert.DoesNotContain(data.ActiveVehicles, v => v.Status == VehicleStatus.Sold);
        Assert.Equal(21469, data.MonthlyFinancials[^1].CapitalInvested); Assert.Equal(14300, data.MonthlyFinancials[^1].SalesRevenue);
        Assert.Equal(-169, data.MonthlyFinancials[^1].RealizedProfit); Assert.Equal(2000, data.MonthlyFinancials[^2].RealizedProfit);
        Assert.Equal(21, factory.Queries.Commands.Count);
    }
}
