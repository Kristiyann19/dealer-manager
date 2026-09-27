using DealerManager.Domain.Enums;

namespace DealerManager.Application.Dtos.Finance
{
    public class FinancialTransactionDto
    {
        public int Id { get; init; }
        public int CapitalAccountId { get; init; }
        public TransactionType Type { get; init; }
        public TransactionDirection Direction { get; init; }
        public decimal Amount { get; init; }
        public string Description { get; init; } = string.Empty;
        public int? VehicleId { get; init; }
        public DateTimeOffset OccurredAt { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }
}
