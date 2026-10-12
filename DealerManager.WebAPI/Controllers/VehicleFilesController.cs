#nullable enable
using DealerManager.Application.Dtos.Vehicle;
using DealerManager.Application.IService.Vehicle;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace DealerManager.Controllers;
[ApiController, Authorize, Route("api/vehicles/{vehicleId:int}")]
public sealed class VehicleFilesController(IVehicleFilesService service) : ControllerBase
{
    [HttpGet("photos")]
    public Task<IReadOnlyList<VehiclePhotoDto>> Photos(int vehicleId, CancellationToken token) => service.Photos(vehicleId, token);
    [HttpPost("photos"), RequestSizeLimit(110 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 110 * 1024 * 1024)]
    public async Task<ActionResult<IReadOnlyList<VehiclePhotoDto>>> UploadPhotos(int vehicleId, [FromForm] PhotoUploadForm form, CancellationToken token)
    {
        if (form.Files.Count is < 1 or > 10) throw new VehicleFileException("photoCount", "Select between 1 and 10 photos.");
        var uploads = form.Files.Select(f => new VehicleFileUpload(f.OpenReadStream(), f.FileName, f.ContentType, f.Length)).ToList();
        try { return StatusCode(201, await service.UploadPhotos(vehicleId, form, uploads, token)); }
        finally { foreach (var upload in uploads) await upload.Stream.DisposeAsync(); }
    }
    [HttpPut("photos/{photoId:int}/primary")]
    public async Task<IActionResult> Primary(int vehicleId, int photoId, CancellationToken token) { await service.SetPrimary(vehicleId, photoId, token); return NoContent(); }
    [HttpDelete("photos/{photoId:int}")]
    public async Task<IActionResult> DeletePhoto(int vehicleId, int photoId, CancellationToken token) { await service.DeletePhoto(vehicleId, photoId, token); return NoContent(); }
    [HttpGet("photos/{photoId:int}/content")]
    public async Task<IActionResult> PhotoContent(int vehicleId, int photoId, CancellationToken token) => PrivateFile(await service.PhotoContent(vehicleId, photoId, token), false);
    [HttpGet("documents")]
    public Task<IReadOnlyList<VehicleDocumentDto>> Documents(int vehicleId, CancellationToken token) => service.Documents(vehicleId, token);
    [HttpPost("documents"), RequestSizeLimit(21 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 21 * 1024 * 1024)]
    public async Task<ActionResult<VehicleDocumentDto>> UploadDocument(int vehicleId, [FromForm] DocumentUploadForm form, CancellationToken token)
    {
        if (form.File == null) throw new VehicleFileException("empty", "A document file is required.");
        await using var stream = form.File.OpenReadStream();
        return StatusCode(201, await service.UploadDocument(vehicleId, form, new(stream, form.File.FileName, form.File.ContentType, form.File.Length), token));
    }
    [HttpPut("documents/{documentId:int}")]
    public Task<VehicleDocumentDto> EditDocument(int vehicleId, int documentId, VehicleDocumentMetadata metadata, CancellationToken token) => service.EditDocument(vehicleId, documentId, metadata, token);
    [HttpDelete("documents/{documentId:int}")]
    public async Task<IActionResult> DeleteDocument(int vehicleId, int documentId, CancellationToken token) { await service.DeleteDocument(vehicleId, documentId, token); return NoContent(); }
    [HttpGet("documents/{documentId:int}/content")]
    public async Task<IActionResult> DocumentContent(int vehicleId, int documentId, [FromQuery] bool download, CancellationToken token) => PrivateFile(await service.DocumentContent(vehicleId, documentId, token), download);
    private IActionResult PrivateFile(VehicleFileContent content, bool download)
    {
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers.XContentTypeOptions = "nosniff";
        Response.Headers.ContentSecurityPolicy = "sandbox";
        return download ? File(content.Stream, content.ContentType, content.FileName) : File(content.Stream, content.ContentType);
    }
}
public sealed class PhotoUploadForm : VehiclePhotoMetadata
{
    public List<IFormFile> Files { get; set; } = [];
}
public sealed class DocumentUploadForm : VehicleDocumentMetadata
{
    public IFormFile? File { get; set; }
}
