using Inventara.Application.Assets;

namespace Inventara.Application.Persistence;

public interface IAssetPhotoStorage
{
    Task<string> SaveAsync(
        Stream content,
        string extension,
        CancellationToken cancellationToken);
}
