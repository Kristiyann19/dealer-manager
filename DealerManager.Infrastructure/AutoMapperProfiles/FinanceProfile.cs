using AutoMapper;
using DealerManager.Application.Dtos.Finance;
using DealerManager.Domain.Entities;

namespace DealerManager.Infrastructure.AutoMapperProfiles;

public sealed class FinanceProfile : Profile
{
    public FinanceProfile()
    {
        CreateMap<CapitalAccount, CapitalAccountDto>()
            .ForMember(d => d.CurrentBalance, o => o.Ignore()).MaxDepth(4);
        CreateMap<CapitalAccountDto, CapitalAccountDetailsDto>()
            .ForMember(d => d.LatestTransactions, o => o.Ignore()).MaxDepth(4);
        CreateMap<FinancialTransaction, FinancialTransactionDto>().MaxDepth(4);
    }
}
