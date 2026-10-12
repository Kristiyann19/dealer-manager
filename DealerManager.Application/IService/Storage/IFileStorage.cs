namespace DealerManager.Application.IService.Storage;
public record StoredFileResult(string StorageKey, long SizeBytes);
public interface IFileStorage
{
    Task<StoredFileResult> SaveAsync(Stream stream, string directory, string extension, long maxBytes, CancellationToken token);
    Task<Stream> OpenReadAsync(string storageKey, CancellationToken token);
    Task DeleteAsync(string storageKey, CancellationToken token);
}
