#nullable enable
using DealerManager.Domain.Enums;

namespace DealerManager.Application.Dtos.Candidate
{
    public class CandidateListDto
    {
        public int Id { get; init; }
        public string Make { get; init; } = string.Empty;
        public string Model { get; init; } = string.Empty;
        public int? Year { get; init; }
        public int? Mileage { get; init; }
        public CandidateStatus Status { get; init; }
        public decimal? AskingPrice { get; init; }
        public decimal? ExpectedSellingPrice { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
        // Null means no estimate exists; zero is a valid calculated result.
        public decimal? EstimatedTotalCost { get; init; }
        public decimal? ExpectedProfit { get; init; }
        public decimal? ExpectedRoi { get; init; }
    }
}
