using Inventara.Application.Persistence;

namespace Inventara.Infrastructure.Storage;

public sealed class FileStorageService(string webRootPath) : IAssetPhotoStorage
{
    public async Task<string> SaveAsync(
        Stream content,
        string extension,
        CancellationToken cancellationToken)
    {
        var safeExtension = extension.ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => ".jpg",
            ".png" => ".png",
            _ => throw new ArgumentOutOfRangeException(nameof(extension), "Unsupported image extension.")
        };
        var storageDirectory = Path.Combine(webRootPath, "uploads", "assets");
        Directory.CreateDirectory(storageDirectory);

        var fileName = $"{Guid.NewGuid():N}{safeExtension}";
        var filePath = Path.Combine(storageDirectory, fileName);
        await using (var destination = new FileStream(
            filePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan))
        {
            await content.CopyToAsync(destination, cancellationToken);
        }

        return $"/uploads/assets/{fileName}";
    }
}
