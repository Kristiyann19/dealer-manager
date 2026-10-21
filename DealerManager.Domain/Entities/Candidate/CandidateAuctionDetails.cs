#nullable enable
using DealerManager.Domain.Common;

namespace DealerManager.Domain.Entities;

// Working auction information, not a winning bid or a financial transaction. Amounts are EUR.
public class CandidateAuctionDetails : Entity
{
    public int CandidateId { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string LotNumber { get; set; } = string.Empty;
    public string? ListingUrl { get; set; }
    public string? Country { get; set; }
    public DateTimeOffset? EndsAt { get; set; }
    public decimal? ObservedBid { get; set; }
    public decimal? MaxBid { get; set; }
    public Candidate Candidate { get; set; } = null!;
}
