namespace Inventara.Application.Common;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int TotalItems,
    int TotalPages,
    int Page,
    int PageSize);
