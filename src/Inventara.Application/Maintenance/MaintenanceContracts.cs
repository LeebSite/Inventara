using Inventara.Domain.Enums;

namespace Inventara.Application.Maintenance;

/// <summary>Filters and pagination options for maintenance tickets.</summary>
public sealed record MaintenanceListQuery
{
    /// <summary>Optional maintenance status filter.</summary>
    public MaintenanceStatus? Status { get; init; }

    /// <summary>Optional asset filter.</summary>
    public Guid? AssetId { get; init; }

    /// <summary>One-based page number.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Maximum records returned per page.</summary>
    public int PageSize { get; init; } = 10;

    /// <summary>Field used to sort matching tickets.</summary>
    public string? SortBy { get; init; }

    /// <summary>Sort direction, either asc or desc.</summary>
    public string SortDir { get; init; } = "desc";
}

/// <summary>Fields used to report asset maintenance.</summary>
public sealed record CreateMaintenanceRequest
{
    /// <summary>Asset requiring maintenance.</summary>
    public required Guid AssetId { get; init; }

    /// <summary>Description of the reported issue.</summary>
    public required string Description { get; init; }

    /// <summary>Optional vendor responsible for maintenance.</summary>
    public Guid? VendorId { get; init; }
}

/// <summary>Fields used to update an active maintenance ticket.</summary>
public sealed record UpdateMaintenanceRequest
{
    /// <summary>New maintenance status. Use the complete action to set Done.</summary>
    public required MaintenanceStatus Status { get; init; }

    /// <summary>Optional maintenance cost.</summary>
    public decimal? Cost { get; init; }

    /// <summary>Updated maintenance completion date.</summary>
    public required DateTimeOffset FinishedAt { get; init; }
}

/// <summary>Maintenance ticket data returned by read and update endpoints.</summary>
public sealed record MaintenanceResponse(
    Guid Id,
    Guid AssetId,
    string AssetCode,
    string AssetName,
    Guid ReportedById,
    DateTimeOffset ReportedAt,
    string Description,
    MaintenanceStatus Status,
    decimal? Cost,
    DateTimeOffset StartedAt,
    DateTimeOffset FinishedAt,
    Guid? VendorId);
