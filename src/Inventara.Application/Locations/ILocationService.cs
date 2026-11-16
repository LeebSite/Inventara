using Inventara.Application.Common;

namespace Inventara.Application.Locations;

public interface ILocationService
{
    Task<PagedResponse<LocationResponse>> GetPageAsync(ListQuery query, CancellationToken cancellationToken);

    Task<LocationResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<LocationResponse> CreateAsync(CreateLocationRequest request, CancellationToken cancellationToken);

    Task<LocationResponse> UpdateAsync(Guid id, UpdateLocationRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
