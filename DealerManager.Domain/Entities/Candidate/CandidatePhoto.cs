using DealerManager.Domain.Common;

namespace DealerManager.Domain.Entities;

public class CandidatePhoto : Entity
{
    public int CandidateId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public DateTimeOffset UploadedAt { get; set; }

    public Candidate Candidate { get; set; } = null!;
}
