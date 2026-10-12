#nullable enable
using DealerManager.Domain.Common;
using DealerManager.Domain.Enums;
namespace DealerManager.Domain.Entities;
public class VehicleReservation : Entity
{
    public int VehicleId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public decimal DepositAmount { get; set; }
    public DateTimeOffset ReservedAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public VehicleReservationStatus Status { get; set; }
    public string? Notes { get; set; }
    public string? CancellationNotes { get; set; }
    public int DepositTransactionId { get; set; }
    public int? RefundTransactionId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public FinancialTransaction DepositTransaction { get; set; } = null!;
    public FinancialTransaction? RefundTransaction { get; set; }
}
