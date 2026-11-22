using Inventara.Domain.Enums;

namespace Inventara.Application.Loans;

/// <summary>Filters and pagination options for loan lists.</summary>
public sealed record LoanListQuery
{
    /// <summary>Optional loan status filter.</summary>
    public LoanStatus? Status { get; init; }

    /// <summary>Inclusive lower bound for loan request time.</summary>
    public DateTimeOffset? RequestedFrom { get; init; }

    /// <summary>Inclusive upper bound for loan request time.</summary>
    public DateTimeOffset? RequestedTo { get; init; }

    /// <summary>Inclusive lower bound for loan date.</summary>
    public DateTimeOffset? LoanFrom { get; init; }

    /// <summary>Inclusive upper bound for loan date.</summary>
    public DateTimeOffset? LoanTo { get; init; }

    /// <summary>Inclusive lower bound for due date.</summary>
    public DateTimeOffset? DueFrom { get; init; }

    /// <summary>Inclusive upper bound for due date.</summary>
    public DateTimeOffset? DueTo { get; init; }

    /// <summary>One-based page number.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Maximum records returned per page.</summary>
    public int PageSize { get; init; } = 10;

    /// <summary>Field used to sort the matching loans.</summary>
    public string? SortBy { get; init; }

    /// <summary>Sort direction, either asc or desc.</summary>
    public string SortDir { get; init; } = "desc";
}

/// <summary>Fields used to request an asset loan.</summary>
public sealed record CreateLoanRequest
{
    /// <summary>Asset requested for the loan.</summary>
    public required Guid AssetId { get; init; }

    /// <summary>Requested return deadline.</summary>
    public required DateTimeOffset DueDate { get; init; }

    /// <summary>Reason for borrowing the asset.</summary>
    public required string Purpose { get; init; }
}

/// <summary>Required reason for rejecting a loan request.</summary>
public sealed record RejectLoanRequest
{
    /// <summary>Reason the request was rejected.</summary>
    public required string Reason { get; init; }
}

/// <summary>Condition recorded when returning a loaned asset.</summary>
public sealed record ReturnLoanRequest
{
    /// <summary>Physical condition observed on return.</summary>
    public required AssetCondition ReturnCondition { get; init; }
}

/// <summary>Loan data returned by read and transition endpoints.</summary>
public sealed record LoanResponse(
    Guid Id,
    Guid AssetId,
    string AssetCode,
    string AssetName,
    Guid BorrowerId,
    string BorrowerName,
    DateTimeOffset RequestedAt,
    DateTimeOffset LoanDate,
    DateTimeOffset DueDate,
    DateTimeOffset? ReturnDate,
    LoanStatus Status,
    string Purpose,
    Guid? ApprovedById,
    string? RejectionReason,
    AssetCondition? ReturnCondition);
