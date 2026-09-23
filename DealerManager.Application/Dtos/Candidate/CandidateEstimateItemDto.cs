using DealerManager.Domain.Enums;

namespace DealerManager.Application.Dtos.Candidate
{
    public class CandidateEstimateItemDto
    {
        public int Id { get; init; }
        public CostCategory Category { get; init; }
        public string Description { get; init; } = string.Empty;
        public decimal EstimatedAmount { get; init; }
    }
}
