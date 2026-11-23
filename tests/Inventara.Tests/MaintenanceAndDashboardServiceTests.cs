using System.Linq.Expressions;
using Inventara.Application.Common;
using Inventara.Application.Common.Exceptions;
using Inventara.Application.Dashboard;
using Inventara.Application.Maintenance;
using Inventara.Application.Persistence;
using Inventara.Application.Validation;
using Inventara.Domain.Common;
using Inventara.Domain.Entities;
using Inventara.Domain.Enums;

namespace Inventara.Tests;

public sealed class MaintenanceAndDashboardServiceTests
{
    [Fact]
    public async Task CreateAsync_ValidTicket_SetsReportedAndMarksAssetUnderMaintenance()
    {
        var asset = new Asset { Status = AssetStatus.Available };
        var repository = new FakeMaintenanceRepository(assets: [asset]);
        var transactions = new FakeTransactionManager();
        var service = CreateMaintenanceService(repository, [asset], transactions: transactions);

        var result = await service.CreateAsync(
            Guid.NewGuid(),
            new CreateMaintenanceRequest
            {
                AssetId = asset.Id,
                Description = "Routine inspection"
            },
            CancellationToken.None);

        var maintenance = Assert.Single(repository.Items);
        Assert.Equal(MaintenanceStatus.Reported, maintenance.Status);
        Assert.Equal(AssetStatus.Maintenance, asset.Status);
        Assert.Equal(maintenance.StartedAt, maintenance.FinishedAt);
        Assert.Equal(maintenance.StartedAt, maintenance.ReportedAt);
        Assert.Equal(maintenance.Id, result.Id);
        Assert.Equal(1, transactions.ExecutionCount);
    }

    [Fact]
    public async Task CreateAsync_AssetNotFound_ThrowsNotFound()
    {
        var service = CreateMaintenanceService();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            service.CreateAsync(
                Guid.NewGuid(),
                new CreateMaintenanceRequest
                {
                    AssetId = Guid.NewGuid(),
                    Description = "Routine inspection"
                },
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_EmptyDescription_FailsValidation()
    {
        var service = CreateMaintenanceService();

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            service.CreateAsync(
                Guid.NewGuid(),
                new CreateMaintenanceRequest
                {
                    AssetId = Guid.NewGuid(),
                    Description = " "
                },
                CancellationToken.None));
    }

    [Fact]
    public async Task GetPageAsync_StatusAndAssetFilters_ReturnsMatchingTickets()
    {
        var targetAssetId = Guid.NewGuid();
        var matching = CreateMaintenance(targetAssetId);
        var other = CreateMaintenance(Guid.NewGuid());
        other.Status = MaintenanceStatus.Done;
        var service = CreateMaintenanceService(maintenances: [matching, other]);

        var page = await service.GetPageAsync(
            new MaintenanceListQuery
            {
                Status = MaintenanceStatus.Reported,
                AssetId = targetAssetId
            },
            CancellationToken.None);

        Assert.Equal(1, page.TotalItems);
        Assert.Equal(matching.Id, Assert.Single(page.Items).Id);
    }

    [Fact]
    public async Task UpdateAsync_ActiveTicket_UpdatesStatusCostAndFinishedAt()
    {
        var maintenance = CreateMaintenance(Guid.NewGuid());
        var service = CreateMaintenanceService(maintenances: [maintenance]);
        var finishedAt = DateTimeOffset.UtcNow.AddDays(1);

        var result = await service.UpdateAsync(
            maintenance.Id,
            new UpdateMaintenanceRequest
            {
                Status = MaintenanceStatus.InProgress,
                Cost = 125,
                FinishedAt = finishedAt
            },
            CancellationToken.None);

        Assert.Equal(MaintenanceStatus.InProgress, result.Status);
        Assert.Equal(125, result.Cost);
        Assert.Equal(finishedAt, result.FinishedAt);
    }

    [Fact]
    public async Task UpdateAsync_DoneStatus_RequiresCompleteAction()
    {
        var maintenance = CreateMaintenance(Guid.NewGuid());
        var service = CreateMaintenanceService(maintenances: [maintenance]);

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            service.UpdateAsync(
                maintenance.Id,
                new UpdateMaintenanceRequest
                {
                    Status = MaintenanceStatus.Done,
                    Cost = 20,
                    FinishedAt = DateTimeOffset.UtcNow
                },
                CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_NegativeCost_FailsValidation()
    {
        var maintenance = CreateMaintenance(Guid.NewGuid());
        var service = CreateMaintenanceService(maintenances: [maintenance]);

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            service.UpdateAsync(
                maintenance.Id,
                new UpdateMaintenanceRequest
                {
                    Status = MaintenanceStatus.InProgress,
                    Cost = -1,
                    FinishedAt = DateTimeOffset.UtcNow
                },
                CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_TicketNotFound_ThrowsNotFound()
    {
        var service = CreateMaintenanceService();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            service.UpdateAsync(
                Guid.NewGuid(),
                new UpdateMaintenanceRequest
                {
                    Status = MaintenanceStatus.InProgress,
                    Cost = 10,
                    FinishedAt = DateTimeOffset.UtcNow
                },
                CancellationToken.None));
    }

    [Fact]
    public async Task CompleteAsync_ActiveTicket_MarksTicketDoneAndAssetAvailable()
    {
        var asset = new Asset { Status = AssetStatus.Maintenance };
        var maintenance = CreateMaintenance(asset.Id);
        maintenance.Status = MaintenanceStatus.InProgress;
        var repository = new FakeMaintenanceRepository([maintenance], [asset]);
        var service = CreateMaintenanceService(repository, [asset]);

        var result = await service.CompleteAsync(maintenance.Id, CancellationToken.None);

        Assert.Equal(MaintenanceStatus.Done, maintenance.Status);
        Assert.Equal(AssetStatus.Available, asset.Status);
        Assert.True(result.FinishedAt >= result.StartedAt);
    }

    [Fact]
    public async Task CompleteAsync_AlreadyCompletedTicket_ThrowsConflict()
    {
        var maintenance = CreateMaintenance(Guid.NewGuid());
        maintenance.Status = MaintenanceStatus.Done;
        var service = CreateMaintenanceService(maintenances: [maintenance]);

        await Assert.ThrowsAsync<ConflictException>(() =>
            service.CompleteAsync(maintenance.Id, CancellationToken.None));
    }

    [Fact]
    public async Task GetSummaryAsync_MissingAssetStatuses_ReturnsZeroCounts()
    {
        var service = new DashboardService(new FakeDashboardRepository(
            new Dictionary<AssetStatus, int>
            {
                [AssetStatus.Available] = 4,
                [AssetStatus.OnLoan] = 2,
                [AssetStatus.Maintenance] = 1
            },
            activeLoans: 3,
            overdueLoans: 1,
            activeMaintenances: 2));

        var summary = await service.GetSummaryAsync(CancellationToken.None);

        Assert.Equal(7, summary.TotalAssets);
        Assert.Equal(4, summary.AvailableAssets);
        Assert.Equal(2, summary.OnLoanAssets);
        Assert.Equal(1, summary.MaintenanceAssets);
        Assert.Equal(0, summary.RetiredAssets);
        Assert.Equal(3, summary.ActiveLoans);
        Assert.Equal(1, summary.OverdueLoans);
        Assert.Equal(2, summary.ActiveMaintenances);
    }

    [Fact]
    public async Task GetAssetsByCategoryAsync_ReturnsRepositoryCounts()
    {
        var category = new DashboardCategoryCount(Guid.NewGuid(), "Laptop", 3);
        var service = new DashboardService(new FakeDashboardRepository(
            new Dictionary<AssetStatus, int>(),
            activeLoans: 0,
            overdueLoans: 0,
            activeMaintenances: 0,
            categories: [category]));

        var result = await service.GetAssetsByCategoryAsync(CancellationToken.None);

        Assert.Equal(new AssetsByCategoryResponse(category.CategoryId, category.CategoryName, 3),
            Assert.Single(result));
    }

    private static MaintenanceService CreateMaintenanceService(
        FakeMaintenanceRepository? repository = null,
        IEnumerable<Asset>? assets = null,
        IEnumerable<Maintenance>? maintenances = null,
        FakeTransactionManager? transactions = null)
    {
        var assetItems = assets?.ToList() ?? [];
        repository ??= new FakeMaintenanceRepository(maintenances, assetItems);
        return new MaintenanceService(
            repository,
            new FakeEntityRepository<Asset>(assetItems),
            new FakeEntityRepository<Vendor>(),
            new MaintenanceListQueryValidator(),
            new CreateMaintenanceRequestValidator(),
            new UpdateMaintenanceRequestValidator(),
            transactions ?? new FakeTransactionManager());
    }

    private static Maintenance CreateMaintenance(Guid assetId) =>
        new()
        {
            AssetId = assetId,
            ReportedById = Guid.NewGuid(),
            ReportedAt = DateTime.UtcNow,
            Description = "Routine inspection",
            Status = MaintenanceStatus.Reported,
            StartedAt = DateTime.UtcNow,
            FinishedAt = DateTime.UtcNow
        };

    private sealed class FakeMaintenanceRepository(
        IEnumerable<Maintenance>? maintenances = null,
        IEnumerable<Asset>? assets = null)
        : FakeEntityRepository<Maintenance>(maintenances), IMaintenanceRepository
    {
        private readonly List<Asset> _assets = assets?.ToList() ?? [];

        public Task<PagedResult<MaintenanceReadRecord>> GetPageAsync(
            MaintenanceFilter filter,
            int page,
            int pageSize,
            string sortBy,
            bool descending,
            CancellationToken cancellationToken)
        {
            var matches = Items
                .Where(maintenance =>
                    (!filter.Status.HasValue || maintenance.Status == filter.Status)
                    && (!filter.AssetId.HasValue || maintenance.AssetId == filter.AssetId))
                .ToArray();
            var pageItems = matches
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(ToReadRecord)
                .ToArray();
            var totalPages = (int)Math.Ceiling(matches.Length / (double)pageSize);
            return Task.FromResult(new PagedResult<MaintenanceReadRecord>(
                pageItems,
                matches.Length,
                totalPages,
                page,
                pageSize));
        }

        public Task<MaintenanceReadRecord?> GetDetailsAsync(
            Guid id,
            CancellationToken cancellationToken)
        {
            var maintenance = Items.FirstOrDefault(item => item.Id == id && !item.IsDeleted);
            return Task.FromResult(maintenance is null ? null : ToReadRecord(maintenance));
        }

        private MaintenanceReadRecord ToReadRecord(Maintenance maintenance)
        {
            var asset = _assets.FirstOrDefault(item => item.Id == maintenance.AssetId);
            return new MaintenanceReadRecord(
                maintenance,
                asset?.AssetCode ?? "INV-LAP-0001",
                asset?.Name ?? "Laptop");
        }
    }

    private class FakeEntityRepository<TEntity>(IEnumerable<TEntity>? entities = null)
        : IEntityRepository<TEntity>
        where TEntity : BaseEntity
    {
        public List<TEntity> Items { get; } = entities?.ToList() ?? [];

        public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(Items.FirstOrDefault(entity => entity.Id == id && !entity.IsDeleted));

        public Task<bool> AnyIncludingDeletedAsync(
            Expression<Func<TEntity, bool>> predicate,
            CancellationToken cancellationToken) =>
            Task.FromResult(Items.Any(predicate.Compile()));

        public Task<PagedResult<TEntity>> GetPageAsync(
            Expression<Func<TEntity, bool>> predicate,
            int page,
            int pageSize,
            string sortBy,
            bool descending,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PagedResult<TEntity>([], 0, 0, page, pageSize));

        public Task AddAsync(TEntity entity, CancellationToken cancellationToken)
        {
            Items.Add(entity);
            return Task.CompletedTask;
        }

        public void Update(TEntity entity)
        {
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
            Task.FromResult(1);
    }

    private sealed class FakeTransactionManager : ITransactionManager
    {
        public int ExecutionCount { get; private set; }

        public Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken)
        {
            ExecutionCount++;
            return operation(cancellationToken);
        }
    }

    private sealed class FakeDashboardRepository(
        IReadOnlyDictionary<AssetStatus, int> assetCounts,
        int activeLoans,
        int overdueLoans,
        int activeMaintenances,
        IReadOnlyList<DashboardCategoryCount>? categories = null) : IDashboardRepository
    {
        public Task<IReadOnlyDictionary<AssetStatus, int>> GetAssetCountsByStatusAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult(assetCounts);

        public Task<int> GetActiveLoanCountAsync(CancellationToken cancellationToken) =>
            Task.FromResult(activeLoans);

        public Task<int> GetOverdueLoanCountAsync(
            DateTime now,
            CancellationToken cancellationToken) =>
            Task.FromResult(overdueLoans);

        public Task<int> GetActiveMaintenanceCountAsync(CancellationToken cancellationToken) =>
            Task.FromResult(activeMaintenances);

        public Task<IReadOnlyList<DashboardCategoryCount>> GetAssetsByCategoryAsync(
            CancellationToken cancellationToken) =>
            Task.FromResult(categories ?? []);
    }
}
