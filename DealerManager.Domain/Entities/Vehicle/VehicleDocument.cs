#nullable enable
using DealerManager.Domain.Common;
using DealerManager.Domain.Enums;
namespace DealerManager.Domain.Entities;
public class VehicleDocument : Entity
{
    public int VehicleId { get; set; }
    public Vehicle Vehicle { get; set; } = null!;
    public VehicleDocumentType Type { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTimeOffset? DocumentDate { get; set; }
    public string? Notes { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public int? UploadedByUserId { get; set; }
}
