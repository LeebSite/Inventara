using Inventara.Application.Persistence;
using Inventara.Domain.Enums;

namespace Inventara.Application.Dashboard;

public sealed class DashboardService(IDashboardRepository dashboard) : IDashboardService
{
    public async Task<DashboardSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var assetCounts = await dashboard.GetAssetCountsByStatusAsync(cancellationToken);
        var activeLoans = await dashboard.GetActiveLoanCountAsync(cancellationToken);
        var overdueLoans = await dashboard.GetOverdueLoanCountAsync(DateTime.UtcNow, cancellationToken);
        var activeMaintenances = await dashboard.GetActiveMaintenanceCountAsync(cancellationToken);

        var availableAssets = GetCount(assetCounts, AssetStatus.Available);
        var onLoanAssets = GetCount(assetCounts, AssetStatus.OnLoan);
        var maintenanceAssets = GetCount(assetCounts, AssetStatus.Maintenance);
        var retiredAssets = GetCount(assetCounts, AssetStatus.Retired);
        return new DashboardSummaryResponse(
            availableAssets + onLoanAssets + maintenanceAssets + retiredAssets,
            availableAssets,
            onLoanAssets,
            maintenanceAssets,
            retiredAssets,
            activeLoans,
            overdueLoans,
            activeMaintenances);
    }

    public async Task<IReadOnlyList<AssetsByCategoryResponse>> GetAssetsByCategoryAsync(
        CancellationToken cancellationToken)
    {
        var results = await dashboard.GetAssetsByCategoryAsync(cancellationToken);
        return results
            .Select(result => new AssetsByCategoryResponse(
                result.CategoryId,
                result.CategoryName,
                result.AssetCount))
            .ToArray();
    }

    private static int GetCount(
        IReadOnlyDictionary<AssetStatus, int> counts,
        AssetStatus status) =>
        counts.TryGetValue(status, out var count) ? count : 0;
}
