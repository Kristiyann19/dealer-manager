using AutoMapper;
using DealerManager.Application.Dtos.Candidate;
using DealerManager.Domain.Entities;

namespace DealerManager.Infrastructure.AutoMapperProfiles;

public sealed class CandidateProfile : Profile
{
    public CandidateProfile()
    {
        CreateMap<CandidateAuctionDetails, CandidateAuctionDetailsDto>();
        CreateMap<CandidateEstimateItem, CandidateEstimateItemDto>().MaxDepth(4);
        CreateMap<CandidateEstimate, CandidateEstimateDto>()
            .ForMember(d => d.Items, o => o.MapFrom(s => s.CandidateEstimateItems.OrderBy(i => i.Id)))
            .ForMember(d => d.FinancialAnalysis, o => o.Ignore())
            .MaxDepth(4);
        CreateMap<Candidate, CandidateListDto>()
            .ForMember(d => d.EstimatedTotalCost, o => o.Ignore())
            .ForMember(d => d.ExpectedProfit, o => o.Ignore())
            .ForMember(d => d.ExpectedRoi, o => o.Ignore())
            .ForMember(d => d.ExpectedSellingPrice, o => o.Ignore())
            .MaxDepth(4);
        CreateMap<Candidate, CandidateDetailsDto>()
            .IncludeBase<Candidate, CandidateListDto>()
            .ForMember(d => d.VehicleId, o => o.MapFrom(s => s.Vehicle == null ? (int?)null : s.Vehicle.Id))
            .ForMember(d => d.EstimateHistory, o => o.Ignore())
            .ForMember(d => d.LatestEstimate, o => o.Ignore())
            .MaxDepth(4);
    }
}
