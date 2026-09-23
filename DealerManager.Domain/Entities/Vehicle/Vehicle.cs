using DealerManager.Domain.Common;
using DealerManager.Domain.Enums;

namespace DealerManager.Domain.Entities;

public class Vehicle : Entity
{
    public int? SourceCandidateId { get; set; }
    public VehicleStatus Status { get; set; }
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public int? Mileage { get; set; }
    public string? Vin { get; set; }
    public DateTimeOffset PurchaseDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Candidate? SourceCandidate { get; set; }
    public ICollection<VehicleStatusHistory> VehicleStatusHistory { get; set; } = new List<VehicleStatusHistory>();
    public ICollection<VehicleCostPlanItem> VehicleCostPlanItems { get; set; } = new List<VehicleCostPlanItem>();
    public ICollection<VehicleExpense> VehicleExpenses { get; set; } = new List<VehicleExpense>();
    public VehicleSale? VehicleSale { get; set; }
}
