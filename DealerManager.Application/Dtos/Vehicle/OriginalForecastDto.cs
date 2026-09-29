using DealerManager.Application.Dtos.Candidate;
namespace DealerManager.Application.Dtos.Vehicle;

public class OriginalForecastDto
{
    public int EstimateId { get; init; }
    public int Version { get; init; }
    public IReadOnlyList<CandidateEstimateItemDto> Items { get; init; } = [];
    public decimal OriginalEstimatedTotal { get; init; }
    public decimal OriginalExpectedSellingPrice { get; init; }
    public decimal OriginalExpectedProfit { get; init; }
    public decimal OriginalExpectedROI { get; init; }
}
