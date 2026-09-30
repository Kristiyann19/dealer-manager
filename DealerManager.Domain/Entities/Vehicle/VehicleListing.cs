using DealerManager.Domain.Common;

namespace DealerManager.Domain.Entities;

public class VehicleListing : Entity
{
    public int VehicleId { get; set; }
    public decimal ListingPrice { get; set; }
    public DateTimeOffset ListedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
}
