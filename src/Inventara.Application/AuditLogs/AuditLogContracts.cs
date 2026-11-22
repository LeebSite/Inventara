using Inventara.Domain.Entities;

namespace Inventara.Application.AuditLogs;

/// <summary>Filters and pagination options for audit log records.</summary>
public sealed record AuditLogQuery
{
    /// <summary>Optional exact entity name filter.</summary>
    public string? EntityName { get; init; }

    /// <summary>Optional acting user ID filter.</summary>
    public Guid? UserId { get; init; }

    /// <summary>Inclusive lower bound for audit creation time.</summary>
    public DateTimeOffset? FromDate { get; init; }

    /// <summary>Inclusive upper bound for audit creation time.</summary>
    public DateTimeOffset? ToDate { get; init; }

    /// <summary>One-based page number.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Maximum records returned per page.</summary>
    public int PageSize { get; init; } = 10;
}

/// <summary>An audit event with serialized values before and after the action.</summary>
public sealed record AuditLogResponse(
    Guid Id,
    Guid? UserId,
    string EntityName,
    Guid EntityId,
    string Action,
    string? OldValues,
    string? NewValues,
    DateTimeOffset CreatedAt);

/// <summary>Fields recorded for an asset audit event.</summary>
public sealed record AssetAuditValues(
    string AssetCode,
    string Name,
    Guid CategoryId,
    Guid LocationId,
    Guid? VendorId,
    string? SerialNumber,
    DateTime PurchaseDate,
    decimal PurchasePrice,
    string Condition,
    string Status,
    string? PhotoUrl,
    string? Notes,
    bool IsDeleted);

/// <summary>Non-sensitive fields recorded for a user audit event.</summary>
public sealed record UserAuditValues(
    string FullName,
    string Email,
    string Role,
    string Department,
    string Phone,
    bool IsActive,
    bool IsDeleted,
    bool PasswordChanged);

/// <summary>Fields recorded for a loan transition audit event.</summary>
public sealed record LoanAuditValues(
    Guid AssetId,
    Guid BorrowerId,
    DateTime RequestedAt,
    DateTime LoanDate,
    DateTime DueDate,
    DateTime? ReturnDate,
    string Status,
    string Purpose,
    Guid? ApprovedById,
    string? RejectionReason,
    string? ReturnCondition);
