using DealerManager.Application.IService.Storage;
using DealerManager.Infrastructure.Storage;
namespace DealerManager.Tests.VehicleFiles;
internal sealed class TestFileStorage : IFileStorage, IDisposable
{
    public string Root { get; } = Path.Combine(Path.GetTempPath(), "DealerManagerFileTests", Guid.NewGuid().ToString("N"));
    private LocalFileStorage Inner => new(Root);
    public bool FailDelete { get; set; }
    public int DeleteAttempts { get; private set; }
    public Task<StoredFileResult> SaveAsync(Stream stream, string directory, string extension, long maxBytes, CancellationToken token) => Inner.SaveAsync(stream, directory, extension, maxBytes, token);
    public Task<Stream> OpenReadAsync(string key, CancellationToken token) => Inner.OpenReadAsync(key, token);
    public Task DeleteAsync(string key, CancellationToken token) { DeleteAttempts++; if (FailDelete) throw new IOException("Injected deletion failure"); return Inner.DeleteAsync(key, token); }
    public string[] Files => Directory.Exists(Root) ? Directory.GetFiles(Root, "*", SearchOption.AllDirectories) : [];
    public void Dispose()
    {
        var safeRoot = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "DealerManagerFileTests")) + Path.DirectorySeparatorChar;
        if (!Path.GetFullPath(Root).StartsWith(safeRoot, StringComparison.OrdinalIgnoreCase)) throw new IOException("Unsafe test cleanup path");
        if (Directory.Exists(Root)) Directory.Delete(Root, true);
    }
}
