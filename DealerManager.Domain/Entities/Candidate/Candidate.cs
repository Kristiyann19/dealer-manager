using DealerManager.Domain.Common;
using DealerManager.Domain.Enums;

namespace DealerManager.Domain.Entities;

public class Candidate : Entity
{
    public CandidateStatus Status { get; set; }
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public int? Year { get; set; }
    public int? Mileage { get; set; }
    public string? Vin { get; set; }
    public string? Source { get; set; }
    public string? Location { get; set; }
    public decimal ExpectedSellingPrice { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RejectedAt { get; set; }
    public DateTimeOffset? PurchasedAt { get; set; }

    public ICollection<CandidatePhoto> CandidatePhotos { get; set; } = new List<CandidatePhoto>();
    public ICollection<CandidateEstimate> CandidateEstimates { get; set; } = new List<CandidateEstimate>();

    // Purchasing creates a Vehicle while preserving this pre-purchase record.
    public Vehicle? Vehicle { get; set; }
}
