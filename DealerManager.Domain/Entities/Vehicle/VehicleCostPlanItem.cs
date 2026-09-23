using DealerManager.Domain.Common;
using DealerManager.Domain.Enums;

namespace DealerManager.Domain.Entities;

// Current post-purchase planning; estimates and commitments do not move capital.
public class VehicleCostPlanItem : Entity
{
    public int VehicleId { get; set; }
    public CostCategory Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal? CurrentEstimatedAmount { get; set; }
    public decimal? CommittedAmount { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool IsCancelled { get; set; }

    public Vehicle Vehicle { get; set; } = null!;
    public ICollection<VehicleExpense> VehicleExpenses { get; set; } = new List<VehicleExpense>();
}
