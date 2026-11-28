using Inventara.Application.Common;
using Inventara.Application.Persistence;
using Inventara.Domain.Entities;
using Inventara.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventara.Infrastructure.Persistence;

public sealed class MaintenanceRepository : EntityRepository<Maintenance>, IMaintenanceRepository
{
    private readonly AppDbContext _dbContext;

    public MaintenanceRepository(AppDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> HasActiveTicketAsync(Guid assetId, CancellationToken cancellationToken) =>
        _dbContext.Maintenances.AnyAsync(
            maintenance => maintenance.AssetId == assetId
                && (maintenance.Status == Domain.Enums.MaintenanceStatus.Reported
                    || maintenance.Status == Domain.Enums.MaintenanceStatus.InProgress),
            cancellationToken);

    public async Task<PagedResult<MaintenanceReadRecord>> GetPageAsync(
        MaintenanceFilter filter,
        int page,
        int pageSize,
        string sortBy,
        bool descending,
        CancellationToken cancellationToken)
    {
        var maintenances = BuildMaintenanceQuery(filter);
        var totalItems = await maintenances.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var offset = ((long)page - 1) * pageSize;
        if (offset > int.MaxValue)
        {
            return new PagedResult<MaintenanceReadRecord>([], totalItems, totalPages, page, pageSize);
        }

        var pagedMaintenances = ApplyOrdering(maintenances, sortBy, descending)
            .ThenBy(maintenance => maintenance.Id)
            .Skip((int)offset)
            .Take(pageSize);
        var items = await BuildReadQuery(pagedMaintenances).ToListAsync(cancellationToken);
        return new PagedResult<MaintenanceReadRecord>(items, totalItems, totalPages, page, pageSize);
    }

    public Task<MaintenanceReadRecord?> GetDetailsAsync(
        Guid id,
        CancellationToken cancellationToken) =>
        BuildReadQuery(_dbContext.Maintenances.AsNoTracking()
                .Where(maintenance => maintenance.Id == id))
            .FirstOrDefaultAsync(cancellationToken);

    private IQueryable<Maintenance> BuildMaintenanceQuery(MaintenanceFilter filter)
    {
        var query = _dbContext.Maintenances.AsNoTracking().AsQueryable();
        if (filter.Status.HasValue)
        {
            query = query.Where(maintenance => maintenance.Status == filter.Status.Value);
        }

        if (filter.AssetId.HasValue)
        {
            query = query.Where(maintenance => maintenance.AssetId == filter.AssetId.Value);
        }

        return query;
    }

    private IQueryable<MaintenanceReadRecord> BuildReadQuery(IQueryable<Maintenance> maintenances) =>
        from maintenance in maintenances
        join asset in _dbContext.Assets.AsNoTracking()
            on maintenance.AssetId equals asset.Id into assets
        from asset in assets.DefaultIfEmpty()
        select new MaintenanceReadRecord(
            maintenance,
            asset == null ? string.Empty : asset.AssetCode,
            asset == null ? string.Empty : asset.Name);

    private static IOrderedQueryable<Maintenance> ApplyOrdering(
        IQueryable<Maintenance> query,
        string sortBy,
        bool descending) =>
        (sortBy.ToLowerInvariant(), descending) switch
        {
            ("id", false) => query.OrderBy(maintenance => maintenance.Id),
            ("id", true) => query.OrderByDescending(maintenance => maintenance.Id),
            ("status", false) => query.OrderBy(maintenance => maintenance.Status),
            ("status", true) => query.OrderByDescending(maintenance => maintenance.Status),
            ("reportedat", false) => query.OrderBy(maintenance => maintenance.ReportedAt),
            ("reportedat", true) => query.OrderByDescending(maintenance => maintenance.ReportedAt),
            ("startedat", false) => query.OrderBy(maintenance => maintenance.StartedAt),
            ("startedat", true) => query.OrderByDescending(maintenance => maintenance.StartedAt),
            ("finishedat", false) => query.OrderBy(maintenance => maintenance.FinishedAt),
            ("finishedat", true) => query.OrderByDescending(maintenance => maintenance.FinishedAt),
            ("cost", false) => query.OrderBy(maintenance => maintenance.Cost),
            ("cost", true) => query.OrderByDescending(maintenance => maintenance.Cost),
            ("createdat", false) => query.OrderBy(maintenance => maintenance.CreatedAt),
            ("createdat", true) => query.OrderByDescending(maintenance => maintenance.CreatedAt),
            _ => throw new InvalidOperationException("The requested sort field is not available.")
        };
}
