using AutoMapper;
using AutoMapper.QueryableExtensions;
#nullable enable
using DealerManager.Application.Dtos.Finance;
using DealerManager.Application.FilterDtos;
using DealerManager.Application.IRepository;
using DealerManager.Application.IService.Finance;
using DealerManager.Domain.Entities;
using DealerManager.Domain.Enums;
using DealerManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace DealerManager.Infrastructure.Service.Finance
{
    public class CapitalAccountService : ICapitalAccountService
    {
        private readonly IBaseRepository<CapitalAccount, FilterDto<CapitalAccount>, DealerManagerDbContext> accounts;
        private readonly IBaseRepository<FinancialTransaction, FilterDto<FinancialTransaction>, DealerManagerDbContext> transactions;
        private readonly IUnitOfWork unitOfWork;
        private readonly TimeProvider timeProvider;
        private readonly IMapper mapper;

        public CapitalAccountService(
            IBaseRepository<CapitalAccount, FilterDto<CapitalAccount>, DealerManagerDbContext> accounts,
            IBaseRepository<FinancialTransaction, FilterDto<FinancialTransaction>, DealerManagerDbContext> transactions,
            IUnitOfWork unitOfWork, TimeProvider timeProvider, IMapper mapper)
        {
            this.accounts = accounts;
            this.transactions = transactions;
            this.unitOfWork = unitOfWork;
            this.timeProvider = timeProvider;
            this.mapper = mapper;
        }

        public async Task<CapitalAccountDto> CreateCapitalAccount(CreateCapitalAccountRequest request, CancellationToken cancellationToken)
        {
            Validate(request);
            cancellationToken.ThrowIfCancellationRequested();
            var account = new CapitalAccount { Name = request.Name.Trim(), Currency = request.Currency.Trim(), IsActive = true };
            await accounts.Create(account);
            await unitOfWork.SaveChanges(cancellationToken);
            return mapper.Map<CapitalAccountDto>(account);
        }

        public async Task<IReadOnlyList<CapitalAccountDto>> GetCapitalAccounts(CancellationToken cancellationToken)
            => await AccountBalances(accounts.GetQueryByProperties(account => account.IsActive))
                .OrderBy(account => account.Id).ToListAsync(cancellationToken);

        public async Task<CapitalAccountDetailsDto> GetCapitalAccountDetails(int id, CancellationToken cancellationToken)
        {
            var account = await AccountBalances(accounts.GetQueryByProperties(account => account.Id == id))
                .SingleOrDefaultAsync(cancellationToken) ?? throw new CapitalAccountNotFoundException(id);
            var latest = await TransactionHistory(id).Take(20).ProjectTo<FinancialTransactionDto>(mapper.ConfigurationProvider).ToListAsync(cancellationToken);
            return mapper.Map(account, new CapitalAccountDetailsDto
            {
                LatestTransactions = latest
            });
        }

        public Task<FinancialTransactionDto> AddCapital(AddCapitalRequest request, CancellationToken cancellationToken)
        {
            Validate(request);
            if (request.CapitalAccountId is not { } accountId)
                throw new ValidationException("CapitalAccountId is required.");
            return unitOfWork.ExecuteInTransaction(async token =>
            {
                var account = await accounts.GetById(accountId, token) ?? throw new CapitalAccountNotFoundException(accountId);
                if (!account.IsActive)
                    throw new CapitalAccountConflictException("Cannot add capital to an inactive account.");
                var now = timeProvider.GetUtcNow();
                var transaction = new FinancialTransaction
                {
                    CapitalAccountId = accountId, Type = TransactionType.CapitalContribution,
                    Direction = TransactionDirection.In, Amount = request.Amount,
                    Description = request.Description?.Trim() ?? string.Empty, OccurredAt = request.OccurredAt?.ToUniversalTime() ?? now,
                    CreatedAt = now
                };
                await transactions.Create(transaction);
                await unitOfWork.SaveChanges(token);
                return mapper.Map<FinancialTransactionDto>(transaction);
            }, cancellationToken);
        }

        public async Task<IReadOnlyList<FinancialTransactionDto>> GetTransactions(int capitalAccountId, CancellationToken cancellationToken)
        {
            if (!await accounts.AnyEntity(account => account.Id == capitalAccountId, cancellationToken))
                throw new CapitalAccountNotFoundException(capitalAccountId);
            return await TransactionHistory(capitalAccountId).ProjectTo<FinancialTransactionDto>(mapper.ConfigurationProvider).ToListAsync(cancellationToken);
        }

        public async Task<decimal> GetBalance(int capitalAccountId, CancellationToken cancellationToken)
            => await AccountBalances(accounts.GetQueryByProperties(a => a.Id == capitalAccountId))
                .Select(a => (decimal?)a.CurrentBalance).SingleOrDefaultAsync(cancellationToken)
                ?? throw new CapitalAccountNotFoundException(capitalAccountId);

        private IQueryable<CapitalAccountDto> AccountBalances(IQueryable<CapitalAccount> query)
            => FinancialQueries.AccountBalances(query, transactions.GetQueryByProperties(_ => true));

        private IOrderedQueryable<FinancialTransaction> TransactionHistory(int id)
            => transactions.GetQueryByProperties(t => t.CapitalAccountId == id)
                .OrderByDescending(t => t.OccurredAt).ThenByDescending(t => t.Id);

        private static void Validate(object? request)
        {
            if (request == null) throw new ValidationException("A request is required.");
            Validator.ValidateObject(request, new ValidationContext(request), validateAllProperties: true);
        }
    }
}
