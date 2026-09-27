using DealerManager.Application.Dtos.Finance;

namespace DealerManager.Application.IService.Finance
{
    public interface ICapitalAccountService
    {
        Task<decimal> GetBalance(int capitalAccountId, CancellationToken cancellationToken);
        Task<CapitalAccountDto> CreateCapitalAccount(CreateCapitalAccountRequest request, CancellationToken cancellationToken);
        Task<IReadOnlyList<CapitalAccountDto>> GetCapitalAccounts(CancellationToken cancellationToken);
        Task<CapitalAccountDetailsDto> GetCapitalAccountDetails(int id, CancellationToken cancellationToken);
        Task<FinancialTransactionDto> AddCapital(AddCapitalRequest request, CancellationToken cancellationToken);
        Task<IReadOnlyList<FinancialTransactionDto>> GetTransactions(int capitalAccountId, CancellationToken cancellationToken);
    }
}
