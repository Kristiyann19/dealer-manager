namespace DealerManager.Application.Dtos.Finance
{
    public class CapitalAccountDetailsDto : CapitalAccountDto
    {
        public IReadOnlyList<FinancialTransactionDto> LatestTransactions { get; init; } = [];
    }
}
