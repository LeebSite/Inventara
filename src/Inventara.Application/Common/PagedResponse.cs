namespace Inventara.Application.Common;

/// <summary>A page of response records and its pagination metadata.</summary>
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalItems,
    int TotalPages,
    int Page,
    int PageSize);
