#nullable enable
namespace DealerManager.Application.Dtos.Candidate
{
    public class CandidateEstimateDto
    {
        public int Id { get; init; }
        public int CandidateId { get; init; }
        public int Version { get; init; }
        public decimal ExpectedSellingPrice { get; init; }
        public string? Notes { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        public bool IsDecisionSnapshot { get; init; }
        public IReadOnlyList<CandidateEstimateItemDto> Items { get; init; } = [];
        public CandidateFinancialAnalysisDto FinancialAnalysis { get; init; } = new();
    }
}
