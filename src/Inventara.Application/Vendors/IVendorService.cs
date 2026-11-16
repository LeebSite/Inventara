using Inventara.Application.Common;

namespace Inventara.Application.Vendors;

public interface IVendorService
{
    Task<PagedResponse<VendorResponse>> GetPageAsync(ListQuery query, CancellationToken cancellationToken);

    Task<VendorResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<VendorResponse> CreateAsync(CreateVendorRequest request, CancellationToken cancellationToken);

    Task<VendorResponse> UpdateAsync(Guid id, UpdateVendorRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
