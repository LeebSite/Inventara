namespace Inventara.Application.Dashboard;

public interface IDashboardService
{
    Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<AssetsByCategoryResponse>> GetAssetsByCategoryAsync(
        CancellationToken cancellationToken);
}
