using System.Linq.Expressions;
namespace DealerManager.Application.Dtos.Vehicle;

// The same expression executes in SQL for aggregates and in memory for individual DTOs.
public static class CostPlanCalculation
{
    public static readonly Expression<Func<VehicleCostPlanItemDto, decimal>> RemainingExpression =
        i => i.IsCancelled ? 0m : Math.Max((i.CommittedAmount ?? i.CurrentEstimatedAmount ?? 0m) - i.ActualPaid, 0m);
    public static readonly Func<VehicleCostPlanItemDto, decimal> Remaining = RemainingExpression.Compile();
}
