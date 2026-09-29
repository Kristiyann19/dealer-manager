#nullable enable
using DealerManager.Application.Dtos.Vehicle;
using DealerManager.Application.FilterDtos.Vehicle;
using DealerManager.Application.IService.Vehicle;
using DealerManager.Domain.Entities;
using DealerManager.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;

namespace DealerManager.Infrastructure.Service.Vehicle;
public partial class VehicleService
{
    public Task<VehicleStatusHistoryDto> ChangeStatus(int vehicleId, ChangeVehicleStatusRequest request, CancellationToken cancellationToken)
    {
        Validate(request);
        return unitOfWork.ExecuteInTransaction(async token =>
        {
            var vehicle = await vehicles.GetById(vehicleId, token, query => query.AsTracking())
                ?? throw new VehicleNotFoundException(vehicleId);
            if (vehicle.Status >= VehicleStatus.Listed)
                throw Conflict("statusLocked", "Listed, reserved and sold vehicles require dedicated business actions.");
            var next = request.Status!.Value;
            if (next > VehicleStatus.ReadyForSale)
                throw Conflict("operationalStatusOnly", "Only operational statuses up to ReadyForSale can be set here.");
            if (next == vehicle.Status) throw Conflict("sameStatus", "The vehicle already has this status.");
            var history = new VehicleStatusHistory
            {
                VehicleId = vehicle.Id, FromStatus = vehicle.Status, ToStatus = next,
                ChangedAt = clock.GetUtcNow(), ChangedByUserId = null, Notes = Normalize(request.Notes)
            };
            vehicle.Status = next;
            await statusHistory.Create(history);
            await unitOfWork.SaveChanges(token);
            return await statusHistory.GetQueryByProperties(h => h.Id == history.Id).Select(HistoryProjection).SingleAsync(token);
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<VehicleStatusHistoryDto>> GetStatusHistory(int vehicleId, CancellationToken cancellationToken)
    {
        await LoadVehicle(vehicleId, cancellationToken);
        return await statusHistory.GetQueryByProperties(h => h.VehicleId == vehicleId)
            .OrderByDescending(h => h.ChangedAt).ThenByDescending(h => h.Id)
            .Select(HistoryProjection).ToListAsync(cancellationToken);
    }

    private static readonly Expression<Func<VehicleStatusHistory, VehicleStatusHistoryDto>> HistoryProjection = h => new()
    { Id = h.Id, FromStatus = h.FromStatus, ToStatus = h.ToStatus, ChangedAt = h.ChangedAt, Notes = h.Notes };

    public async Task<VehicleListResultDto> GetVehicles(VehicleFilterDto filter, CancellationToken cancellationToken)
    {
        filter ??= new();
        Validate(filter);
        if (filter.Offset < 0 || (!filter.GetAllData && (filter.Limit < 1 || filter.Limit > 500)))
            throw new ValidationException("Offset must be non-negative and Limit must be between 1 and 500.");
        var (page, total) = await vehicles.GetAll(filter, cancellationToken, orderBy: q => q.OrderByDescending(v => v.Id));
        if (page.Count == 0) return new() { TotalCount = total };
        var ids = page.Select(v => v.Id).ToArray();
        var candidateIds = page.Where(v => v.SourceCandidateId.HasValue).Select(v => v.SourceCandidateId!.Value).ToArray();

        // Batch aggregates for the selected page: query count does not grow with inventory size.
        var purchases = await transactions.GetQueryByProperties(t => t.VehicleId.HasValue && ids.Contains(t.VehicleId.Value)
                && t.Type == TransactionType.VehiclePurchase && t.Direction == TransactionDirection.Out)
            .GroupBy(t => t.VehicleId!.Value).Select(g => new { Id = g.Key, Amount = g.Sum(t => t.Amount) })
            .ToDictionaryAsync(x => x.Id, x => x.Amount, cancellationToken);
        var paid = await expenses.GetQueryByProperties(e => ids.Contains(e.VehicleId))
            .GroupBy(e => e.VehicleId).Select(g => new { Id = g.Key, Amount = g.Sum(e => e.Amount) })
            .ToDictionaryAsync(x => x.Id, x => x.Amount, cancellationToken);
        var forecasts = await estimates.GetQueryByProperties(e => candidateIds.Contains(e.CandidateId) && e.IsDecisionSnapshot)
            .Select(e => new { e.CandidateId, e.ExpectedSellingPrice }).ToDictionaryAsync(e => e.CandidateId, e => e.ExpectedSellingPrice, cancellationToken);
        var costPlans = await plans.GetQueryByProperties(i => ids.Contains(i.VehicleId))
            .Select(i => new { i.VehicleId, Plan = new VehicleCostPlanItemDto {
                CurrentEstimatedAmount = i.CurrentEstimatedAmount, CommittedAmount = i.CommittedAmount,
                IsCancelled = i.IsCancelled, ActualPaid = i.VehicleExpenses.Sum(e => (decimal?)e.Amount) ?? 0m
            } }).ToListAsync(cancellationToken);
        var plansByVehicle = costPlans.ToLookup(i => i.VehicleId, i => i.Plan);
        return new()
        {
            TotalCount = total,
            Items = page.Select(vehicle =>
            {
                decimal? selling = vehicle.SourceCandidateId is { } source && forecasts.TryGetValue(source, out var price) ? price : null;
                var summary = CalculateFinancialSummary(purchases.GetValueOrDefault(vehicle.Id), paid.GetValueOrDefault(vehicle.Id),
                    plansByVehicle[vehicle.Id], selling);
                return new VehicleListItemDto
                {
                    Id = vehicle.Id, Make = vehicle.Make, Model = vehicle.Model, Year = vehicle.Year,
                    Mileage = vehicle.Mileage, Vin = vehicle.Vin, Status = vehicle.Status, PurchaseDate = vehicle.PurchaseDate,
                    ActualPurchasePrice = summary.ActualPurchasePrice, ActualExpenses = summary.ActualExpenses,
                    TotalInvested = summary.TotalInvested, RemainingProjectedCosts = summary.RemainingProjectedCosts,
                    ProjectedFinalCost = summary.ProjectedFinalCost, ExpectedSellingPrice = summary.ExpectedSellingPrice,
                    ProjectedProfit = summary.ProjectedProfit, ProjectedROI = summary.ProjectedROI
                };
            }).ToList()
        };
    }
}
