using Inventara.Application.Dashboard;
using Inventara.Domain.Enums;

namespace Inventara.Application.Persistence;

public interface IDashboardRepository
{
    Task<IReadOnlyDictionary<AssetStatus, int>> GetAssetCountsByStatusAsync(
        CancellationToken cancellationToken);

    Task<int> GetActiveLoanCountAsync(CancellationToken cancellationToken);

    Task<int> GetOverdueLoanCountAsync(DateTime now, CancellationToken cancellationToken);

    Task<int> GetActiveMaintenanceCountAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<DashboardCategoryCount>> GetAssetsByCategoryAsync(
        CancellationToken cancellationToken);
}
