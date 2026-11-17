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
        var query = BuildQuery(filter);
        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var offset = ((long)page - 1) * pageSize;
        if (offset > int.MaxValue)
        {
            return new PagedResult<AssetReadRecord>([], totalItems, totalPages, page, pageSize);
        }

        var orderedQuery = ApplyOrdering(query, sortBy, descending);
        var items = await orderedQuery
            .ThenBy(record => record.Asset.Id)
            .Skip((int)offset)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<AssetReadRecord>(items, totalItems, totalPages, page, pageSize);
    }

    public async Task<AssetDetailsRecord?> GetDetailsAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var asset = await BuildQuery(new AssetFilter(null, null, null, null))
            .FirstOrDefaultAsync(record => record.Asset.Id == id, cancellationToken);
        if (asset is null)
        {
            return null;
        }

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

        return new AssetDetailsRecord(asset, mostRecentLoan);
    }

    public async Task<IReadOnlyList<AssetReadRecord>> GetAssetsForExportAsync(
        AssetFilter filter,
        string sortBy,
        bool descending,
        CancellationToken cancellationToken)
    {
        var orderedQuery = ApplyOrdering(BuildQuery(filter), sortBy, descending)
            .ThenBy(record => record.Asset.Id);
        return await orderedQuery.ToListAsync(cancellationToken);
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

    private IQueryable<AssetReadRecord> BuildQuery(AssetFilter filter)
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

        return query.Select(asset => new AssetReadRecord(
            asset,
            _dbContext.Categories
                .Where(category => category.Id == asset.CategoryId)
                .Select(category => category.Name)
                .FirstOrDefault() ?? string.Empty,
            _dbContext.Locations
                .Where(location => location.Id == asset.LocationId)
                .Select(location => location.Name)
                .FirstOrDefault() ?? string.Empty,
            _dbContext.Vendors
                .Where(vendor => vendor.Id == asset.VendorId)
                .Select(vendor => vendor.Name)
                .FirstOrDefault()));
    }

    private static IOrderedQueryable<AssetReadRecord> ApplyOrdering(
        IQueryable<AssetReadRecord> query,
        string sortBy,
        bool descending)
    {
        return (sortBy.ToLowerInvariant(), descending) switch
        {
            ("id", false) => query.OrderBy(record => record.Asset.Id),
            ("id", true) => query.OrderByDescending(record => record.Asset.Id),
            ("name", false) => query.OrderBy(record => record.Asset.Name),
            ("name", true) => query.OrderByDescending(record => record.Asset.Name),
            ("purchasedate", false) => query.OrderBy(record => record.Asset.PurchaseDate),
            ("purchasedate", true) => query.OrderByDescending(record => record.Asset.PurchaseDate),
            ("purchaseprice", false) => query.OrderBy(record => record.Asset.PurchasePrice),
            ("purchaseprice", true) => query.OrderByDescending(record => record.Asset.PurchasePrice),
            ("condition", false) => query.OrderBy(record => record.Asset.Condition),
            ("condition", true) => query.OrderByDescending(record => record.Asset.Condition),
            ("status", false) => query.OrderBy(record => record.Asset.Status),
            ("status", true) => query.OrderByDescending(record => record.Asset.Status),
            ("createdat", false) => query.OrderBy(record => record.Asset.CreatedAt),
            ("createdat", true) => query.OrderByDescending(record => record.Asset.CreatedAt),
            ("assetcode", false) => query.OrderBy(record => record.Asset.AssetCode),
            ("assetcode", true) => query.OrderByDescending(record => record.Asset.AssetCode),
            _ => throw new InvalidOperationException("The requested sort field is not available.")
        };
    }
}
