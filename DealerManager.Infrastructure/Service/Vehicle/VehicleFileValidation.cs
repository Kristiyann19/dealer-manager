using DealerManager.Application.Dtos.Vehicle;
using DealerManager.Application.IService.Vehicle;
namespace DealerManager.Infrastructure.Service.Vehicle;
internal static class VehicleFileValidation
{
    internal const long PhotoLimit = 10 * 1024 * 1024;
    internal const long DocumentLimit = 20 * 1024 * 1024;
    internal static async Task<(string Name, string Extension, string Mime)> Validate(VehicleFileUpload file, bool photo, CancellationToken token)
    {
        var limit = photo ? PhotoLimit : DocumentLimit;
        if (file.Length <= 0) throw new VehicleFileException("empty", "The file is empty.");
        if (file.Length > limit) throw new VehicleFileException("tooLarge", "File exceeds the size limit.");
        var name = Path.GetFileName(file.FileName.Replace('\\', '/'));
        if (string.IsNullOrWhiteSpace(name) || name.Length > 255 || name.Any(char.IsControl)) throw new VehicleFileException("invalidName", "Invalid file name.");
        var extension = Path.GetExtension(name).ToLowerInvariant();
        var mime = extension switch { ".jpg" or ".jpeg" => "image/jpeg", ".png" => "image/png", ".webp" when photo => "image/webp", ".pdf" when !photo => "application/pdf", _ => "" };
        if (mime == "" || !string.Equals(file.ContentType, mime, StringComparison.OrdinalIgnoreCase))
            throw new VehicleFileException("unsupported", "Unsupported file extension or content type.");
        if (!file.Stream.CanSeek) throw new VehicleFileException("invalidContent", "A seekable upload stream is required.");
        file.Stream.Position = 0;
        var header = new byte[12]; var count = await file.Stream.ReadAtLeastAsync(header, 12, false, token);
        file.Stream.Position = 0;
        bool matches = mime switch {
            "image/jpeg" => count >= 3 && header[0] == 0xff && header[1] == 0xd8 && header[2] == 0xff,
            "image/png" => count >= 8 && header.AsSpan(0,8).SequenceEqual(new byte[] {137,80,78,71,13,10,26,10}),
            "image/webp" => count >= 12 && header.AsSpan(0,4).SequenceEqual("RIFF"u8) && header.AsSpan(8,4).SequenceEqual("WEBP"u8),
            "application/pdf" => count >= 5 && header.AsSpan(0,5).SequenceEqual("%PDF-"u8), _ => false
        };
        if (!matches) throw new VehicleFileException("invalidContent", "File signature does not match its type.");
        return (name, extension, mime);
    }
}
