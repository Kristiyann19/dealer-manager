using DriveType = DealerManager.Domain.Enums.DriveType;
using DealerManager.Domain.Common;
using DealerManager.Domain.Enums;

namespace DealerManager.Domain.Entities;

public class Vehicle : Entity, ITenantEntity
{
    public int DealershipId { get; set; }
    public int? SourceCandidateId { get; set; }
    public VehicleStatus Status { get; set; }
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int Year { get; set; }
    public int? Mileage { get; set; }
    public string? Vin { get; set; }
    public string? RegistrationNumber { get; set; }
    public DateOnly? FirstRegistration { get; set; }
    public FuelType? FuelType { get; set; }
    public Transmission? Transmission { get; set; }
    public int? EngineDisplacementCc { get; set; }
    public int? PowerKw { get; set; }
    public int? PowerHp { get; set; }
    public DriveType? DriveType { get; set; }
    public EuroStandard? EuroStandard { get; set; }
    public BodyType? BodyType { get; set; }
    public string? Color { get; set; }
    public int? NumberOfDoors { get; set; }
    public int? NumberOfSeats { get; set; }
    public int? NumberOfKeys { get; set; }
    public string? ImportedFrom { get; set; }
    public string? Notes { get; set; }

    public DateTimeOffset PurchaseDate { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public Candidate? SourceCandidate { get; set; }
    public ICollection<VehicleStatusHistory> VehicleStatusHistory { get; set; } = new List<VehicleStatusHistory>();
    public ICollection<VehicleCostPlanItem> VehicleCostPlanItems { get; set; } = new List<VehicleCostPlanItem>();
    public ICollection<VehicleExpense> VehicleExpenses { get; set; } = new List<VehicleExpense>();
    public VehicleSale? VehicleSale { get; set; }
    public ICollection<VehicleListing> VehicleListings { get; set; } = new List<VehicleListing>();
}
