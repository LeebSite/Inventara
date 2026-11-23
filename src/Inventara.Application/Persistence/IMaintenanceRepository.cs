using Inventara.Application.Common;
using Inventara.Domain.Enums;
using MaintenanceEntity = Inventara.Domain.Entities.Maintenance;

namespace Inventara.Application.Persistence;

public interface IMaintenanceRepository : IEntityRepository<MaintenanceEntity>
{
    Task<PagedResult<MaintenanceReadRecord>> GetPageAsync(
        MaintenanceFilter filter,
        int page,
        int pageSize,
        string sortBy,
        bool descending,
        CancellationToken cancellationToken);

    Task<MaintenanceReadRecord?> GetDetailsAsync(Guid id, CancellationToken cancellationToken);
}

public sealed record MaintenanceFilter(MaintenanceStatus? Status, Guid? AssetId);

public sealed record MaintenanceReadRecord(
    MaintenanceEntity Maintenance,
    string AssetCode,
    string AssetName);
