using DealerManager.Domain.Common;
using DealerManager.Domain.Enums;

namespace DealerManager.Domain.Entities;

public class CandidateEstimateItem : Entity
{
    public int CandidateEstimateId { get; set; }
    public CostCategory Category { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal EstimatedAmount { get; set; }

    public CandidateEstimate CandidateEstimate { get; set; } = null!;
}
