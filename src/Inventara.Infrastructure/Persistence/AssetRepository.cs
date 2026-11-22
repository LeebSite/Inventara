using System.Data;
using Inventara.Application.Common;
using Inventara.Application.Common.Exceptions;
using Inventara.Application.Persistence;
using Inventara.Domain.Entities;
using Inventara.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventara.Infrastructure.Persistence;

public sealed class AssetRepository : EntityRepository<Asset>, IAssetRepository
{
    private readonly AppDbContext _dbContext;

    public AssetRepository(AppDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<AssetReadRecord>> GetAssetPageAsync(
        AssetFilter filter,
        int page,
        int pageSize,
        string sortBy,
        bool descending,
        CancellationToken cancellationToken)
    {
        var assets = BuildAssetQuery(filter);
        var totalItems = await assets.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var offset = ((long)page - 1) * pageSize;
        if (offset > int.MaxValue)
        {
            return new PagedResult<AssetReadRecord>([], totalItems, totalPages, page, pageSize);
        }

        var orderedAssets = ApplyOrdering(assets, sortBy, descending)
            .ThenBy(asset => asset.Id);
        var pagedAssets = orderedAssets
            .Skip((int)offset)
            .Take(pageSize);
        var items = await BuildQuery(pagedAssets).ToListAsync(cancellationToken);

        return new PagedResult<AssetReadRecord>(items, totalItems, totalPages, page, pageSize);
    }

    public async Task<AssetDetailsRecord?> GetDetailsAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var asset = await BuildAssetQuery(new AssetFilter(null, null, null, null))
            .FirstOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (asset is null)
        {
            return null;
        }

        var categoryName = await _dbContext.Categories
            .AsNoTracking()
            .Where(category => category.Id == asset.CategoryId)
            .Select(category => category.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
        var locationName = await _dbContext.Locations
            .AsNoTracking()
            .Where(location => location.Id == asset.LocationId)
            .Select(location => location.Name)
            .FirstOrDefaultAsync(cancellationToken) ?? string.Empty;
        var vendorName = asset.VendorId.HasValue
            ? await _dbContext.Vendors
                .AsNoTracking()
                .Where(vendor => vendor.Id == asset.VendorId.Value)
                .Select(vendor => vendor.Name)
                .FirstOrDefaultAsync(cancellationToken)
            : null;
        var assetRecord = new AssetReadRecord(asset, categoryName, locationName, vendorName);

        var mostRecentLoan = await _dbContext.Loans
            .AsNoTracking()
            .Where(loan => loan.AssetId == id)
            .OrderByDescending(loan => loan.RequestedAt)
            .ThenByDescending(loan => loan.Id)
            .Select(loan => new AssetRecentLoanRecord(
                loan.Id,
                loan.BorrowerId,
                loan.RequestedAt,
                loan.LoanDate,
                loan.DueDate,
                loan.ReturnDate,
                loan.Status,
                loan.Purpose))
            .FirstOrDefaultAsync(cancellationToken);

        return new AssetDetailsRecord(assetRecord, mostRecentLoan);
    }

    public async Task<IReadOnlyList<AssetReadRecord>> GetAssetsForExportAsync(
        AssetFilter filter,
        string sortBy,
        bool descending,
        CancellationToken cancellationToken)
    {
        var orderedAssets = ApplyOrdering(BuildAssetQuery(filter), sortBy, descending)
            .ThenBy(asset => asset.Id);
        return await BuildQuery(orderedAssets).ToListAsync(cancellationToken);
    }

    public async Task AddWithGeneratedCodeAsync(
        Asset asset,
        string codePrefix,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var existingCodes = await _dbContext.Assets
            .IgnoreQueryFilters()
            .Where(existingAsset => existingAsset.AssetCode.StartsWith(codePrefix + "-"))
            .Select(existingAsset => existingAsset.AssetCode)
            .ToListAsync(cancellationToken);

        var lastSequence = existingCodes
            .Select(code => int.TryParse(code.AsSpan(code.LastIndexOf('-') + 1), out var sequence)
                ? sequence
                : 0)
            .DefaultIfEmpty()
            .Max();
        if (lastSequence == int.MaxValue)
        {
            throw new ConflictException("No asset codes remain for this category prefix.");
        }

        asset.AssetCode = $"{codePrefix}-{lastSequence + 1:D4}";
        await AddAsync(asset, cancellationToken);
        await SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private IQueryable<Asset> BuildAssetQuery(AssetFilter filter)
    {
        var query = _dbContext.Assets.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(asset =>
                asset.Name.Contains(search) || asset.AssetCode.Contains(search));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(asset => asset.Status == filter.Status.Value);
        }

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(asset => asset.CategoryId == filter.CategoryId.Value);
        }

        if (filter.LocationId.HasValue)
        {
            query = query.Where(asset => asset.LocationId == filter.LocationId.Value);
        }

        return query;
    }

    private IQueryable<AssetReadRecord> BuildQuery(IQueryable<Asset> assets) =>
        from asset in assets
        join category in _dbContext.Categories.AsNoTracking()
            on asset.CategoryId equals category.Id into categories
        from category in categories.DefaultIfEmpty()
        join location in _dbContext.Locations.AsNoTracking()
            on asset.LocationId equals location.Id into locations
        from location in locations.DefaultIfEmpty()
        join vendor in _dbContext.Vendors.AsNoTracking()
            on asset.VendorId equals (Guid?)vendor.Id into vendors
        from vendor in vendors.DefaultIfEmpty()
        select new AssetReadRecord(
            asset,
            category == null ? string.Empty : category.Name,
            location == null ? string.Empty : location.Name,
            vendor == null ? null : vendor.Name);

    private static IOrderedQueryable<Asset> ApplyOrdering(
        IQueryable<Asset> query,
        string sortBy,
        bool descending)
    {
        return (sortBy.ToLowerInvariant(), descending) switch
        {
            ("id", false) => query.OrderBy(asset => asset.Id),
            ("id", true) => query.OrderByDescending(asset => asset.Id),
            ("name", false) => query.OrderBy(asset => asset.Name),
            ("name", true) => query.OrderByDescending(asset => asset.Name),
            ("purchasedate", false) => query.OrderBy(asset => asset.PurchaseDate),
            ("purchasedate", true) => query.OrderByDescending(asset => asset.PurchaseDate),
            ("purchaseprice", false) => query.OrderBy(asset => asset.PurchasePrice),
            ("purchaseprice", true) => query.OrderByDescending(asset => asset.PurchasePrice),
            ("condition", false) => query.OrderBy(asset => asset.Condition),
            ("condition", true) => query.OrderByDescending(asset => asset.Condition),
            ("status", false) => query.OrderBy(asset => asset.Status),
            ("status", true) => query.OrderByDescending(asset => asset.Status),
            ("createdat", false) => query.OrderBy(asset => asset.CreatedAt),
            ("createdat", true) => query.OrderByDescending(asset => asset.CreatedAt),
            ("assetcode", false) => query.OrderBy(asset => asset.AssetCode),
            ("assetcode", true) => query.OrderByDescending(asset => asset.AssetCode),
            _ => throw new InvalidOperationException("The requested sort field is not available.")
        };
    }
}
