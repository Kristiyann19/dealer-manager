#nullable enable
namespace DealerManager.Application.Dtos.Candidate
{
    public class CandidateDetailsDto : CandidateListDto
    {
        public string? Vin { get; init; }
        public string? Source { get; init; }
        public string? Location { get; init; }
        public string? Notes { get; init; }
        public DateTimeOffset? RejectedAt { get; init; }
        public DateTimeOffset? PurchasedAt { get; init; }
        public IReadOnlyList<CandidateEstimateDto> EstimateHistory { get; init; } = [];
        public CandidateEstimateDto? LatestEstimate { get; init; }
    }
}
