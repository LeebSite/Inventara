using Inventara.Application.Dashboard;
using Inventara.Application.Persistence;
using Inventara.Domain.Enums;
using Inventara.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventara.Infrastructure.Persistence;

public sealed class DashboardRepository(AppDbContext dbContext) : IDashboardRepository
{
    public async Task<IReadOnlyDictionary<AssetStatus, int>> GetAssetCountsByStatusAsync(
        CancellationToken cancellationToken) =>
        await dbContext.Assets
            .AsNoTracking()
            .GroupBy(asset => asset.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(result => result.Status, result => result.Count, cancellationToken);

    public Task<int> GetActiveLoanCountAsync(CancellationToken cancellationToken) =>
        dbContext.Loans.CountAsync(
            loan => loan.Status == LoanStatus.Approved || loan.Status == LoanStatus.OnLoan,
            cancellationToken);

    public Task<int> GetOverdueLoanCountAsync(
        DateTime now,
        CancellationToken cancellationToken) =>
        dbContext.Loans.CountAsync(
            loan => loan.Status == LoanStatus.OnLoan && loan.DueDate < now,
            cancellationToken);

    public Task<int> GetActiveMaintenanceCountAsync(CancellationToken cancellationToken) =>
        dbContext.Maintenances.CountAsync(
            maintenance => maintenance.Status == MaintenanceStatus.Reported
                || maintenance.Status == MaintenanceStatus.InProgress,
            cancellationToken);

    public async Task<IReadOnlyList<DashboardCategoryCount>> GetAssetsByCategoryAsync(
        CancellationToken cancellationToken) =>
        await dbContext.Categories
            .AsNoTracking()
            .GroupJoin(
                dbContext.Assets.AsNoTracking(),
                category => category.Id,
                asset => asset.CategoryId,
                (category, assets) => new
                {
                    category.Id,
                    category.Name,
                    AssetCount = assets.Count()
                })
            .OrderBy(category => category.Name)
            .Select(category => new DashboardCategoryCount(
                category.Id,
                category.Name,
                category.AssetCount))
            .ToListAsync(cancellationToken);
}
