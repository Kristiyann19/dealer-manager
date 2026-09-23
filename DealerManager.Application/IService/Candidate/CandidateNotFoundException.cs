namespace DealerManager.Application.IService.Candidate
{
    public class CandidateNotFoundException(int candidateId)
        : Exception($"Candidate {candidateId} was not found.")
    {
    }
}
