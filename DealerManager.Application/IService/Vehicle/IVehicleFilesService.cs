using DealerManager.Application.Dtos.Vehicle;
namespace DealerManager.Application.IService.Vehicle;
public interface IVehicleFilesService
{
    Task<IReadOnlyList<VehiclePhotoDto>> Photos(int vehicleId, CancellationToken token);
    Task<IReadOnlyList<VehiclePhotoDto>> UploadPhotos(int vehicleId, VehiclePhotoMetadata metadata, IReadOnlyList<VehicleFileUpload> files, CancellationToken token);
    Task SetPrimary(int vehicleId, int photoId, CancellationToken token);
    Task DeletePhoto(int vehicleId, int photoId, CancellationToken token);
    Task<VehicleFileContent> PhotoContent(int vehicleId, int photoId, CancellationToken token);
    Task<IReadOnlyList<VehicleDocumentDto>> Documents(int vehicleId, CancellationToken token);
    Task<VehicleDocumentDto> UploadDocument(int vehicleId, VehicleDocumentMetadata metadata, VehicleFileUpload file, CancellationToken token);
    Task<VehicleDocumentDto> EditDocument(int vehicleId, int documentId, VehicleDocumentMetadata metadata, CancellationToken token);
    Task DeleteDocument(int vehicleId, int documentId, CancellationToken token);
    Task<VehicleFileContent> DocumentContent(int vehicleId, int documentId, CancellationToken token);
}
public class VehicleFileException(string code, string message, int statusCode = 400) : Exception(message)
{
    public string Code { get; } = code;
    public int StatusCode { get; } = statusCode;
}
