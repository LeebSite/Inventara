using System.Linq.Expressions;
using Inventara.Application.AuditLogs;
using Inventara.Application.Assets;
using Inventara.Application.Common;
using Inventara.Application.Common.Exceptions;
using Inventara.Application.Persistence;
using Inventara.Application.Validation;
using Inventara.Domain.Common;
using Inventara.Domain.Entities;
using Inventara.Domain.Enums;
using Inventara.Infrastructure.Storage;
using ClosedXML.Excel;
using System.Text.Json;

namespace Inventara.Tests;

public sealed class AssetServiceTests
{
    private static readonly Guid LocationId = Guid.NewGuid();

    [Fact]
    public async Task CreateAsync_ValidAsset_GeneratesCategoryBasedCode()
    {
        var category = new Category { Name = "Laptop" };
        var repository = new FakeAssetRepository();
        var auditLogs = new FakeAuditLogService();
        var service = CreateService(repository, [category], auditLogs: auditLogs);

        var result = await service.CreateAsync(
            Guid.NewGuid(),
            CreateRequest(category.Id),
            CancellationToken.None);

        Assert.Equal("INV-LAP-0001", result.AssetCode);
        Assert.Equal("INV-LAP", repository.LastCodePrefix);
        var audit = Assert.Single(auditLogs.Entries);
        Assert.Equal("Asset", audit.EntityName);
        Assert.Null(audit.OldValues);
        Assert.Contains("INV-LAP-0001", audit.NewValues);
    }

    [Fact]
    public async Task GetPageAsync_FilteredQuery_ForwardsFiltersAndPagination()
    {
        var repository = new FakeAssetRepository();
        var service = CreateService(repository);

        var result = await service.GetPageAsync(
            new AssetListQuery
            {
                Search = " laptop ",
                Status = AssetStatus.Available,
                CategoryId = Guid.NewGuid(),
                LocationId = LocationId,
                Page = 2,
                PageSize = 5,
                SortBy = "name",
                SortDir = "desc"
            },
            CancellationToken.None);

        Assert.Equal("laptop", repository.LastFilter?.Search);
        Assert.Equal(AssetStatus.Available, repository.LastFilter?.Status);
        Assert.Equal(2, result.Page);
        Assert.Equal(5, result.PageSize);
    }

    [Fact]
    public async Task GetByIdAsync_MissingAsset_ThrowsNotFound()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            service.GetByIdAsync(Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task DeleteAsync_ExistingAsset_SoftDeletesAsset()
    {
        var asset = new Asset { AssetCode = "INV-LAP-0001" };
        var repository = new FakeAssetRepository([asset]);
        var service = CreateService(repository);

        await service.DeleteAsync(asset.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.True(asset.IsDeleted);
    }

    [Fact]
    public async Task CreateAsync_MissingCategory_ThrowsNotFound()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            service.CreateAsync(Guid.NewGuid(), CreateRequest(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task UpdateAsync_ExistingAsset_UpdatesFields()
    {
        var asset = new Asset { AssetCode = "INV-LAP-0001", Name = "Old name" };
        var category = new Category();
        var service = CreateService(new FakeAssetRepository([asset]), [category]);

        var result = await service.UpdateAsync(
            asset.Id,
            Guid.NewGuid(),
            CreateUpdateRequest() with { CategoryId = category.Id, Name = "Updated name" },
            CancellationToken.None);

        Assert.Equal("Updated name", asset.Name);
        Assert.Equal("INV-LAP-0001", result.AssetCode);
    }

    [Fact]
    public async Task UpdateAsync_MissingAsset_ThrowsNotFound()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            service.UpdateAsync(Guid.NewGuid(), Guid.NewGuid(), CreateUpdateRequest(), CancellationToken.None));
    }

    [Fact]
    public async Task UploadPhotoAsync_InvalidImageSignature_ThrowsValidation()
    {
        var asset = new Asset { AssetCode = "INV-LAP-0001" };
        var repository = new FakeAssetRepository([asset]);
        var storage = new FakePhotoStorage();
        var service = CreateService(repository, photoStorage: storage);
        using var content = new MemoryStream([1, 2, 3, 4]);

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            service.UploadPhotoAsync(
                asset.Id,
                Guid.NewGuid(),
                new UploadAssetPhotoRequest("photo.jpg", "image/jpeg", content.Length, content),
                CancellationToken.None));

        Assert.False(storage.WasCalled);
    }

    [Fact]
    public async Task UploadPhotoAsync_ValidPng_SavesPhotoAndUpdatesAsset()
    {
        var asset = new Asset { AssetCode = "INV-LAP-0001" };
        var repository = new FakeAssetRepository([asset]);
        var storage = new FakePhotoStorage();
        var service = CreateService(repository, photoStorage: storage);
        using var content = new MemoryStream([137, 80, 78, 71, 13, 10, 26, 10]);

        var result = await service.UploadPhotoAsync(
            asset.Id,
            Guid.NewGuid(),
            new UploadAssetPhotoRequest("photo.png", "image/png", content.Length, content),
            CancellationToken.None);

        Assert.True(storage.WasCalled);
        Assert.Equal("/uploads/assets/photo.png", asset.PhotoUrl);
        Assert.Equal(asset.PhotoUrl, result.PhotoUrl);
    }

    [Fact]
    public async Task ExportAsync_UnsupportedSortField_FailsValidation()
    {
        var service = CreateService();

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() =>
            service.ExportAsync(
                new AssetListQuery { SortBy = "unknown" },
                CancellationToken.None));
    }

    [Fact]
    public async Task CreateAssetRequestValidator_NegativePurchasePrice_FailsValidation()
    {
        var request = CreateRequest(Guid.NewGuid()) with { PurchasePrice = -0.01m };

        var result = await new CreateAssetRequestValidator().ValidateAsync(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error =>
            error.PropertyName == nameof(CreateAssetRequest.PurchasePrice));
    }

    [Fact]
    public void ClosedXmlAssetExporter_Export_WritesAssetWorksheetAndHeaders()
    {
        var content = new ClosedXmlAssetExporter().Export(
        [
            new AssetExportRow(
                "INV-LAP-0001",
                "Laptop",
                "Laptop",
                "Office",
                null,
                null,
                new DateTime(2026, 1, 1),
                1000m,
                AssetCondition.Good,
                AssetStatus.Available,
                null,
                null)
        ]);
        using var stream = new MemoryStream(content);
        using var workbook = new XLWorkbook(stream);

        Assert.Equal("Assets", workbook.Worksheet(1).Name);
        Assert.Equal("Asset Code", workbook.Worksheet(1).Cell(1, 1).GetString());
        Assert.Equal("INV-LAP-0001", workbook.Worksheet(1).Cell(2, 1).GetString());
    }

    private static CreateAssetRequest CreateRequest(Guid categoryId) =>
        new()
        {
            Name = "Laptop",
            CategoryId = categoryId,
            LocationId = AssetServiceTests.LocationId,
            PurchaseDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            PurchasePrice = 1000m,
            Condition = AssetCondition.Good,
            Status = AssetStatus.Available
        };

    private static UpdateAssetRequest CreateUpdateRequest() =>
        new()
        {
            Name = "Updated Laptop",
            CategoryId = Guid.NewGuid(),
            LocationId = LocationId,
            PurchaseDate = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            PurchasePrice = 900m,
            Condition = AssetCondition.Good,
            Status = AssetStatus.Available
        };

    private static AssetService CreateService(
        FakeAssetRepository? assetRepository = null,
        IEnumerable<Category>? categories = null,
        FakePhotoStorage? photoStorage = null,
        FakeAuditLogService? auditLogs = null) =>
        new(
            assetRepository ?? new FakeAssetRepository(),
            new FakeRepository<Category>(categories),
            new FakeRepository<Location>([new Location { Id = LocationId }]),
            new FakeRepository<Vendor>(),
            new AssetListQueryValidator(),
            new CreateAssetRequestValidator(),
            new UpdateAssetRequestValidator(),
            new UploadAssetPhotoRequestValidator(),
            photoStorage ?? new FakePhotoStorage(),
            new FakeExcelExporter(),
            auditLogs ?? new FakeAuditLogService(),
            new FakeTransactionManager());

    private sealed class FakeAuditLogService : IAuditLogService
    {
        public List<AuditLogEntry> Entries { get; } = [];

        public Task AddAsync(
            Guid? userId,
            string entityName,
            Guid entityId,
            string action,
            object? oldValues,
            object? newValues,
            CancellationToken cancellationToken)
        {
            Entries.Add(new AuditLogEntry(
                userId,
                entityName,
                entityId,
                action,
                oldValues is null ? null : JsonSerializer.Serialize(oldValues),
                newValues is null ? null : JsonSerializer.Serialize(newValues)));
            return Task.CompletedTask;
        }

        public Task<PagedResponse<AuditLogResponse>> GetPageAsync(
            AuditLogQuery query,
            CancellationToken cancellationToken) =>
            Task.FromResult(new PagedResponse<AuditLogResponse>([], 0, 0, query.Page, query.PageSize));

        public sealed record AuditLogEntry(
            Guid? UserId,
            string EntityName,
            Guid EntityId,
            string Action,
            string? OldValues,
            string? NewValues);
    }

    private sealed class FakeTransactionManager : ITransactionManager
    {
        public Task ExecuteAsync(
            Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken) =>
            operation(cancellationToken);
    }

    private sealed class FakeAssetRepository(IEnumerable<Asset>? assets = null)
        : FakeRepository<Asset>(assets), IAssetRepository
    {
        public string? LastCodePrefix { get; private set; }

        public AssetFilter? LastFilter { get; private set; }

        public Task<PagedResult<AssetReadRecord>> GetAssetPageAsync(
            AssetFilter filter,
            int page,
            int pageSize,
            string sortBy,
            bool descending,
            CancellationToken cancellationToken)
        {
            LastFilter = filter;
            return Task.FromResult(new PagedResult<AssetReadRecord>([], 0, 0, page, pageSize));
        }

        public Task<AssetDetailsRecord?> GetDetailsAsync(Guid id, CancellationToken cancellationToken)
        {
            var asset = Items.FirstOrDefault(item => item.Id == id && !item.IsDeleted);
            return Task.FromResult(asset is null
                ? null
                : new AssetDetailsRecord(
                    new AssetReadRecord(asset, "Laptop", "Office", null),
                    null));
        }

        public Task<IReadOnlyList<AssetReadRecord>> GetAssetsForExportAsync(
            AssetFilter filter,
            string sortBy,
            bool descending,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<AssetReadRecord>>([]);

        public Task AddWithGeneratedCodeAsync(
            Asset asset,
            string codePrefix,
            CancellationToken cancellationToken)
        {
            LastCodePrefix = codePrefix;
            asset.AssetCode = $"{codePrefix}-0001";
            Items.Add(asset);
            return Task.CompletedTask;
        }
    }

    private sealed class FakePhotoStorage : IAssetPhotoStorage
    {
        public bool WasCalled { get; private set; }

        public Task<string> SaveAsync(
            Stream content,
            string extension,
            CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult($"/uploads/assets/photo.{extension.TrimStart('.')}");
        }
    }

    private sealed class FakeExcelExporter : IAssetExcelExporter
    {
        public byte[] Export(IReadOnlyList<AssetExportRow> assets) => [];
    }

    private class FakeRepository<TEntity>(IEnumerable<TEntity>? entities = null)
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
}
