namespace DealerManager.Application.Dtos.Candidate
{
    public class CandidateListResultDto
    {
        public IReadOnlyList<CandidateListDto> Items { get; init; } = [];
        public int TotalCount { get; init; }
    }
}
