#nullable enable
using DealerManager.Domain.Enums;
namespace DealerManager.Application.Dtos.Vehicle;
public class VehicleReservationDto
{
    public int ReservationId { get; init; }
    public int VehicleId { get; init; }
    public string CustomerName { get; init; } = string.Empty;
    public string? CustomerPhone { get; init; }
    public string? CustomerEmail { get; init; }
    public decimal DepositAmount { get; init; }
    public DateTimeOffset ReservedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public VehicleReservationStatus Status { get; init; }
    public VehicleStatus VehicleStatus { get; init; }
    public string? Notes { get; init; }
    public string? CancellationNotes { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public DateTimeOffset? CancelledAt { get; init; }
    public int DepositTransactionId { get; init; }
    public int? RefundTransactionId { get; init; }
    public int CapitalAccountId { get; init; }
}
