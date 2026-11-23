using Inventara.Application.Common;

namespace Inventara.Application.Maintenance;

public interface IMaintenanceService
{
    Task<PagedResponse<MaintenanceResponse>> GetPageAsync(
        MaintenanceListQuery query,
        CancellationToken cancellationToken);

    Task<MaintenanceResponse> CreateAsync(
        Guid reporterId,
        CreateMaintenanceRequest request,
        CancellationToken cancellationToken);

    Task<MaintenanceResponse> UpdateAsync(
        Guid id,
        UpdateMaintenanceRequest request,
        CancellationToken cancellationToken);

    Task<MaintenanceResponse> CompleteAsync(
        Guid id,
        CancellationToken cancellationToken);
}
