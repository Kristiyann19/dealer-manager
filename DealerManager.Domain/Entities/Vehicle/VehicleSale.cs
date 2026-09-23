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
    /// References the incoming financial transaction recording the sale payment.
    /// </summary>
    public int FinancialTransactionId { get; set; }

    public Vehicle Vehicle { get; set; } = null!;
    public FinancialTransaction FinancialTransaction { get; set; } = null!;
}
