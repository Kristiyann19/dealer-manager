using DealerManager.Domain.Common;
using DealerManager.Domain.Enums;

namespace DealerManager.Domain.Entities;

// Actual spending, backed by a capital transaction.
public class VehicleExpense : Entity
{
    public int VehicleId { get; set; }
    public int? CostPlanItemId { get; set; }
    public CostCategory Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTimeOffset PaidAt { get; set; }
    public string? Supplier { get; set; }
    public string? DocumentNumber { get; set; }
    public int FinancialTransactionId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    // The original payment is retained; corrections append a reversal and optionally a replacement.
    public bool IsReversed { get; set; }
    public DateTimeOffset? ReversedAt { get; set; }
    public string? ReversalReason { get; set; }
    public int? ReversedByUserId { get; set; }
    public int? ReversalTransactionId { get; set; }
    public int? ReplacementExpenseId { get; set; }
    public FinancialTransaction? ReversalTransaction { get; set; }
    public VehicleExpense? ReplacementExpense { get; set; }

    public Vehicle Vehicle { get; set; } = null!;
    public VehicleCostPlanItem? CostPlanItem { get; set; }
    public FinancialTransaction FinancialTransaction { get; set; } = null!;
}
