#nullable enable
using DealerManager.Domain.Enums;
namespace DealerManager.Application.Dtos.Vehicle;

public class VehicleDetailsDto
{
    public int Id { get; init; }
    public string Make { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public int Year { get; init; }
    public VehicleStatus Status { get; init; }
    public int? SourceCandidateId { get; init; }
    public DateTimeOffset PurchaseDate { get; init; }
    public decimal? ActualPurchasePrice { get; init; }
}
