using System.Linq.Expressions;
using FluentValidation;
using Inventara.Application.Common;
using Inventara.Application.Common.Exceptions;
using Inventara.Application.Persistence;
using Inventara.Domain.Entities;

namespace Inventara.Application.Locations;

public sealed class LocationService(
    IEntityRepository<Location> locations,
    IValidator<ListQuery> listQueryValidator,
    IValidator<CreateLocationRequest> createValidator,
    IValidator<UpdateLocationRequest> updateValidator) : ILocationService
{
    public async Task<PagedResponse<LocationResponse>> GetPageAsync(
        ListQuery query,
        CancellationToken cancellationToken)
    {
        await listQueryValidator.ValidateAndThrowAsync(query, cancellationToken);
        var search = query.Search?.Trim();
        Expression<Func<Location, bool>> predicate = string.IsNullOrEmpty(search)
            ? location => true
            : location => location.Name.Contains(search)
                || location.Building.Contains(search)
                || location.Floor.Contains(search)
                || location.Description.Contains(search);
        var sortBy = ServiceHelpers.ResolveSortField(
            query.SortBy,
            nameof(Location.Name),
            ("id", nameof(Location.Id)),
            ("name", nameof(Location.Name)),
            ("building", nameof(Location.Building)),
            ("floor", nameof(Location.Floor)),
            ("description", nameof(Location.Description)),
            ("createdAt", nameof(Location.CreatedAt)));

        var result = await locations.GetPageAsync(
            predicate,
            query.Page,
            query.PageSize,
            sortBy,
            query.SortDir.Equals("desc", StringComparison.OrdinalIgnoreCase),
            cancellationToken);

        return ServiceHelpers.MapPage(result, ToResponse);
    }

    public async Task<LocationResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var location = await locations.GetByIdAsync(id, cancellationToken);
        return location is null
            ? throw new EntityNotFoundException("Location not found.")
            : ToResponse(location);
    }

    public async Task<LocationResponse> CreateAsync(
        CreateLocationRequest request,
        CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);
        var location = new Location
        {
            Name = request.Name.Trim(),
            Building = request.Building.Trim(),
            Floor = request.Floor.Trim(),
            Description = request.Description.Trim()
        };

        await locations.AddAsync(location, cancellationToken);
        await locations.SaveChangesAsync(cancellationToken);
        return ToResponse(location);
    }

    public async Task<LocationResponse> UpdateAsync(
        Guid id,
        UpdateLocationRequest request,
        CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);
        var location = await locations.GetByIdAsync(id, cancellationToken);
        if (location is null)
        {
            throw new EntityNotFoundException("Location not found.");
        }

        location.Name = request.Name.Trim();
        location.Building = request.Building.Trim();
        location.Floor = request.Floor.Trim();
        location.Description = request.Description.Trim();
        locations.Update(location);
        await locations.SaveChangesAsync(cancellationToken);
        return ToResponse(location);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var location = await locations.GetByIdAsync(id, cancellationToken);
        if (location is null)
        {
            throw new EntityNotFoundException("Location not found.");
        }

        location.IsDeleted = true;
        locations.Update(location);
        await locations.SaveChangesAsync(cancellationToken);
    }

    private static LocationResponse ToResponse(Location location) =>
        new(
            location.Id,
            location.Name,
            location.Building,
            location.Floor,
            location.Description);
}
