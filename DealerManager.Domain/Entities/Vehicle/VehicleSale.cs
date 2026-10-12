#nullable enable
using DealerManager.Domain.Common;

namespace DealerManager.Domain.Entities;

public class VehicleSale : Entity
{
    public int VehicleId { get; set; }
    public decimal SalePrice { get; set; }
    public DateTimeOffset SoldAt { get; set; }
    public int? CustomerId { get; set; }
    public string? Notes { get; set; }

    /// <summary>
    /// References the final payment, absent when the deposit covers the full sale price.
    /// </summary>
    public int? FinancialTransactionId { get; set; }

    public Vehicle Vehicle { get; set; } = null!;
    public FinancialTransaction? FinancialTransaction { get; set; }
    public int? ReservationId { get; set; }
    public VehicleReservation? Reservation { get; set; }
}
