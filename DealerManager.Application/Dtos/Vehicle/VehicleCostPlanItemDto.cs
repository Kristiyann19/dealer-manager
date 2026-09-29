#nullable enable
using DealerManager.Domain.Enums;
namespace DealerManager.Application.Dtos.Vehicle;

public class VehicleCostPlanItemDto
{
    public int Id { get; init; }
    public CostCategory Category { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal? CurrentEstimatedAmount { get; init; }
    public decimal? CommittedAmount { get; init; }
    public decimal ActualPaid { get; init; }
    public decimal RemainingProjected => IsCancelled ? 0m : Math.Max((CommittedAmount ?? CurrentEstimatedAmount ?? 0m) - ActualPaid, 0m);
    public bool IsCancelled { get; init; }
}
