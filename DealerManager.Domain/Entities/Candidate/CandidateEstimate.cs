using DealerManager.Domain.Common;

namespace DealerManager.Domain.Entities;

// Versioned pre-purchase analysis; estimates do not move capital.
public class CandidateEstimate : Entity
{
    public int CandidateId { get; set; }
    public int Version { get; set; }
    public decimal ExpectedSellingPrice { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// Marks the historical estimate used to approve purchase.
    /// Once selected, this estimate and its items must be preserved rather than overwritten.
    /// </summary>
    public bool IsDecisionSnapshot { get; set; }

    public Candidate Candidate { get; set; } = null!;
    public ICollection<CandidateEstimateItem> CandidateEstimateItems { get; set; } = new List<CandidateEstimateItem>();
}
