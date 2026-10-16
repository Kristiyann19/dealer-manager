#nullable enable
using DealerManager.Domain.Enums;
namespace DealerManager.Application.Dtos.Vehicle;

public class VehicleExpenseDto
{
    public bool IsReversed { get; init; }
    public DateTimeOffset? ReversedAt { get; init; }
    public string? ReversalReason { get; init; }
    public int? ReversedByUserId { get; init; }
    public int? ReversalTransactionId { get; init; }
    public int? ReplacementExpenseId { get; init; }
    public int Id { get; init; }
    public CostCategory Category { get; init; }
    public string Description { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string? Supplier { get; init; }
    public string? DocumentNumber { get; init; }
    public DateTimeOffset PaidAt { get; init; }
    public int? CostPlanItemId { get; init; }
    public string? CostPlanDescription { get; init; }
    public int FinancialTransactionId { get; init; }
    public int CapitalAccountId { get; init; }
    public string CapitalAccountName { get; init; } = string.Empty;
    public string Currency { get; init; } = string.Empty;
}
