using DealerManager.Domain.Enums;

namespace DealerManager.Application.Dtos.Vehicle;

public class VehicleListingDto
{
    public int VehicleId { get; init; }
    public int ListingId { get; init; }
    public decimal ListingPrice { get; init; }
    public DateTimeOffset ListedAt { get; init; }
    public VehicleStatus Status { get; init; }
}
