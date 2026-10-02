using DriveType = DealerManager.Domain.Enums.DriveType;
#nullable enable
using DealerManager.Domain.Enums;
namespace DealerManager.Application.Dtos.Vehicle;

public class VehicleDetailsDto : VehicleFinancialSummaryDto
{
    public int Id { get; init; }
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int Year { get; init; }
    public int? Mileage { get; init; }
    public string? Vin { get; init; }
    public string? RegistrationNumber { get; init; }
    public DateOnly? FirstRegistration { get; init; }
    public FuelType? FuelType { get; init; }
    public Transmission? Transmission { get; init; }
    public int? EngineDisplacementCc { get; init; }
    public int? PowerKw { get; init; }
    public int? PowerHp { get; init; }
    public DriveType? DriveType { get; init; }
    public EuroStandard? EuroStandard { get; init; }
    public BodyType? BodyType { get; init; }
    public string? Color { get; init; }
    public int? NumberOfDoors { get; init; }
    public int? NumberOfSeats { get; init; }
    public int? NumberOfKeys { get; init; }
    public string? ImportedFrom { get; init; }
    public string? Notes { get; init; }

    public VehicleStatus Status { get; init; }
    public int? SourceCandidateId { get; init; }
    public DateTimeOffset PurchaseDate { get; init; }
    public OriginalForecastDto? OriginalForecast { get; init; }
    public VehicleListingDto? CurrentListing { get; init; }
    public decimal? ListingPrice { get; init; }
    public DateTimeOffset? ListedAt { get; init; }
    public VehiclePaymentAccountDto? SaleAccount { get; init; }
}
