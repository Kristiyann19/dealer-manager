using DealerManager.Application.Dtos.Candidate;
using DealerManager.Application.FilterDtos.Candidate;

namespace DealerManager.Application.IService.Candidate
{
    public interface ICandidateService
    {
        Task<CandidateDetailsDto> CreateCandidate(CreateCandidateRequest request, CancellationToken cancellationToken);
        Task<CandidateListResultDto> GetCandidates(CandidateFilterDto filter, CancellationToken cancellationToken);
        Task<CandidateDetailsDto> GetCandidateDetails(int id, CancellationToken cancellationToken);
        Task<CandidateEstimateDto> CreateCandidateEstimate(CreateCandidateEstimateRequest request, CancellationToken cancellationToken);
        Task<IReadOnlyList<CandidateEstimateDto>> GetEstimateHistory(int candidateId, CancellationToken cancellationToken);
        Task<CandidateDetailsDto> ApproveCandidate(int id, CancellationToken cancellationToken);
        Task<CandidateDetailsDto> RejectCandidate(int id, RejectCandidateRequest request, CancellationToken cancellationToken);
    }
}
