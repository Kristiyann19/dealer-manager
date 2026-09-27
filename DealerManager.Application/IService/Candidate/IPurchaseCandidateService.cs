using DealerManager.Application.Dtos.Candidate;

namespace DealerManager.Application.IService.Candidate;

public interface IPurchaseCandidateService
{
    Task<PurchaseCandidateResultDto> PurchaseCandidate(int candidateId, PurchaseCandidateRequest request, CancellationToken cancellationToken);
}
