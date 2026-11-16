using FluentValidation;
using FluentValidation.Results;

namespace Inventara.Application.Common;

internal static class ServiceHelpers
{
    public static string ResolveSortField(
        string? requestedField,
        string defaultField,
        params (string RequestName, string EntityProperty)[] allowedFields)
    {
        if (string.IsNullOrWhiteSpace(requestedField))
        {
            return defaultField;
        }

        var match = allowedFields.FirstOrDefault(field =>
            field.RequestName.Equals(requestedField, StringComparison.OrdinalIgnoreCase));

        if (match == default)
        {
            throw new ValidationException(
                [new ValidationFailure("sortBy", "Sort field is not supported.")]);
        }

        return match.EntityProperty;
    }

    public static PagedResponse<TResponse> MapPage<TEntity, TResponse>(
        PagedResult<TEntity> page,
        Func<TEntity, TResponse> map) =>
        new(
            page.Items.Select(map).ToArray(),
            page.TotalItems,
            page.TotalPages,
            page.Page,
            page.PageSize);

    public static DateTimeOffset ToUtcOffset(DateTime value) =>
        new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
