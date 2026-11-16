using System.Linq.Expressions;
using FluentValidation;
using Inventara.Application.Common;
using Inventara.Application.Common.Exceptions;
using Inventara.Application.Persistence;
using Inventara.Domain.Entities;

namespace Inventara.Application.Vendors;

public sealed class VendorService(
    IEntityRepository<Vendor> vendors,
    IValidator<ListQuery> listQueryValidator,
    IValidator<CreateVendorRequest> createValidator,
    IValidator<UpdateVendorRequest> updateValidator) : IVendorService
{
    public async Task<PagedResponse<VendorResponse>> GetPageAsync(
        ListQuery query,
        CancellationToken cancellationToken)
    {
        await listQueryValidator.ValidateAndThrowAsync(query, cancellationToken);
        var search = query.Search?.Trim();
        Expression<Func<Vendor, bool>> predicate = string.IsNullOrEmpty(search)
            ? vendor => true
            : vendor => vendor.Name.Contains(search)
                || vendor.ContactPerson.Contains(search)
                || vendor.Email.Contains(search)
                || vendor.Phone.Contains(search)
                || vendor.Address.Contains(search);
        var sortBy = ServiceHelpers.ResolveSortField(
            query.SortBy,
            nameof(Vendor.Name),
            ("id", nameof(Vendor.Id)),
            ("name", nameof(Vendor.Name)),
            ("contactPerson", nameof(Vendor.ContactPerson)),
            ("email", nameof(Vendor.Email)),
            ("phone", nameof(Vendor.Phone)),
            ("createdAt", nameof(Vendor.CreatedAt)));

        var result = await vendors.GetPageAsync(
            predicate,
            query.Page,
            query.PageSize,
            sortBy,
            query.SortDir.Equals("desc", StringComparison.OrdinalIgnoreCase),
            cancellationToken);

        return ServiceHelpers.MapPage(result, ToResponse);
    }

    public async Task<VendorResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var vendor = await vendors.GetByIdAsync(id, cancellationToken);
        return vendor is null
            ? throw new EntityNotFoundException("Vendor not found.")
            : ToResponse(vendor);
    }

    public async Task<VendorResponse> CreateAsync(
        CreateVendorRequest request,
        CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var vendor = new Vendor
        {
            Name = request.Name.Trim(),
            ContactPerson = request.ContactPerson.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            Phone = request.Phone.Trim(),
            Address = request.Address.Trim()
        };

        await vendors.AddAsync(vendor, cancellationToken);
        await vendors.SaveChangesAsync(cancellationToken);
        return ToResponse(vendor);
    }

    public async Task<VendorResponse> UpdateAsync(
        Guid id,
        UpdateVendorRequest request,
        CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var vendor = await vendors.GetByIdAsync(id, cancellationToken);
        if (vendor is null)
        {
            throw new EntityNotFoundException("Vendor not found.");
        }

        vendor.Name = request.Name.Trim();
        vendor.ContactPerson = request.ContactPerson.Trim();
        vendor.Email = request.Email.Trim().ToLowerInvariant();
        vendor.Phone = request.Phone.Trim();
        vendor.Address = request.Address.Trim();
        vendors.Update(vendor);
        await vendors.SaveChangesAsync(cancellationToken);
        return ToResponse(vendor);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var vendor = await vendors.GetByIdAsync(id, cancellationToken);
        if (vendor is null)
        {
            throw new EntityNotFoundException("Vendor not found.");
        }

        vendor.IsDeleted = true;
        vendors.Update(vendor);
        await vendors.SaveChangesAsync(cancellationToken);
    }

    private static VendorResponse ToResponse(Vendor vendor) =>
        new(
            vendor.Id,
            vendor.Name,
            vendor.ContactPerson,
            vendor.Email,
            vendor.Phone,
            vendor.Address);
}
