using DealerManager.Application.IService.Storage;
using DealerManager.Application.IService.Vehicle;
namespace DealerManager.Infrastructure.Storage;
public sealed class LocalFileStorage : IFileStorage
{
    private readonly string root;
    public LocalFileStorage(string rootPath) => root = Path.GetFullPath(rootPath);
    private string Resolve(string key)
    {
        // Keys are server-generated relative paths, never user filenames or URLs.
        if (string.IsNullOrWhiteSpace(key) || Path.IsPathRooted(key) || key.Contains('\\') || key.Contains(':')
            || key.Split('/').Any(p => p is "" or "." or "..")) throw new IOException("Invalid storage key.");
        var path = Path.GetFullPath(Path.Combine(root, key));
        if (!path.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new IOException("Storage path outside root.");
        // Do not traverse symlinks/junctions, including a configured root symlink.
        for (var current = path; !string.IsNullOrEmpty(current); current = Path.GetDirectoryName(current))
            if ((File.Exists(current) || Directory.Exists(current)) && (File.GetAttributes(current) & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Storage symlinks are not supported.");
        return path;
    }
    public async Task<StoredFileResult> SaveAsync(Stream stream, string directory, string extension, long maxBytes, CancellationToken token)
    {
        if (!new[] { ".jpg", ".jpeg", ".png", ".webp", ".pdf" }.Contains(extension)) throw new IOException("Invalid storage extension.");
        var key = $"{directory}/{Guid.NewGuid():N}{extension}";
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        try
        {
            await using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
            var buffer = new byte[81920]; long size = 0; int count;
            while ((count = await stream.ReadAsync(buffer, token)) > 0)
            {
                size += count;
                if (size > maxBytes) throw new VehicleFileException("tooLarge", "File exceeds the size limit.");
                await file.WriteAsync(buffer.AsMemory(0, count), token);
            }
            if (size == 0) throw new VehicleFileException("empty", "The file is empty.");
            return new(key, size);
        }
        catch { File.Delete(path); throw; }
    }
    public Task<Stream> OpenReadAsync(string storageKey, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        return Task.FromResult<Stream>(new FileStream(Resolve(storageKey), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true));
    }
    public Task DeleteAsync(string storageKey, CancellationToken token)
    {
        token.ThrowIfCancellationRequested(); File.Delete(Resolve(storageKey)); return Task.CompletedTask;
    }
}
