using DealerManager.Domain.Common;
using DealerManager.Domain.Enums;

namespace DealerManager.Domain.Entities;

public class VehicleStatusHistory : Entity
{
    public int VehicleId { get; set; }
    public VehicleStatus FromStatus { get; set; }
    public VehicleStatus ToStatus { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public int? ChangedByUserId { get; set; }
    public string? Notes { get; set; }

    public Vehicle Vehicle { get; set; } = null!;
}
