using Inventara.Application.Common;

namespace Inventara.Application.Assets;

public interface IAssetService
{
    Task<PagedResponse<AssetResponse>> GetPageAsync(
        AssetListQuery query,
        CancellationToken cancellationToken);

    Task<AssetDetailResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<AssetResponse> CreateAsync(
        CreateAssetRequest request,
        CancellationToken cancellationToken);

    Task<AssetResponse> UpdateAsync(
        Guid id,
        UpdateAssetRequest request,
        CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);

    Task<AssetResponse> UploadPhotoAsync(
        Guid id,
        UploadAssetPhotoRequest request,
        CancellationToken cancellationToken);

    Task<byte[]> ExportAsync(AssetListQuery query, CancellationToken cancellationToken);
}
