#nullable enable
using DealerManager.Domain.Enums;
namespace DealerManager.Application.Dtos.Vehicle;
public class VehicleStatusHistoryDto
{
    public int Id { get; init; }
    public VehicleStatus FromStatus { get; init; }
    public VehicleStatus ToStatus { get; init; }
    public DateTimeOffset ChangedAt { get; init; }
    public string? Notes { get; init; }
}
