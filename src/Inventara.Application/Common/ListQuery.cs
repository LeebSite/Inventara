namespace Inventara.Application.Common;

/// <summary>Pagination, search, and sorting options for a list endpoint.</summary>
public sealed record ListQuery
{
    /// <summary>Optional text used to filter matching records.</summary>
    public string? Search { get; init; }

    /// <summary>One-based page number.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Maximum records returned per page.</summary>
    public int PageSize { get; init; } = 10;

    /// <summary>Optional resource-specific sort field.</summary>
    public string? SortBy { get; init; }

    /// <summary>Sort direction, either asc or desc.</summary>
    public string SortDir { get; init; } = "asc";
}
