#nullable enable
using System.ComponentModel.DataAnnotations;
using DealerManager.Domain.Enums;
namespace DealerManager.Application.Dtos.Vehicle;
// Streams are opened by the transport and disposed there. No ASP.NET dependency in Application.
public record VehicleFileUpload(Stream Stream, string FileName, string ContentType, long Length);
public record VehicleFileContent(Stream Stream, string FileName, string ContentType);
public class VehiclePhotoMetadata
{
    [Required, EnumDataType(typeof(VehiclePhotoType))] public VehiclePhotoType? Type { get; set; }
    [StringLength(1000)] public string? Caption { get; set; }
}
public class VehicleDocumentMetadata
{
    [Required, EnumDataType(typeof(VehicleDocumentType))] public VehicleDocumentType? Type { get; set; }
    [Required, StringLength(200)] public string Title { get; set; } = string.Empty;
    public DateTimeOffset? DocumentDate { get; set; }
    [StringLength(2000)] public string? Notes { get; set; }
}
