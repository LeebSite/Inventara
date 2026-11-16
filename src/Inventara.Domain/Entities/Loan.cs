using Inventara.Domain.Common;
using Inventara.Domain.Enums;

namespace Inventara.Domain.Entities;

public class Loan : BaseEntity
{
    public Guid AssetId { get; set; }

    public Guid BorrowerId { get; set; }

    public DateTime RequestedAt { get; set; }

    public DateTime LoanDate { get; set; }

    public DateTime DueDate { get; set; }

    public DateTime? ReturnDate { get; set; }

    public LoanStatus Status { get; set; }

    public string Purpose { get; set; } = string.Empty;

    public Guid? ApprovedById { get; set; }

    public string? RejectionReason { get; set; }

    public AssetCondition? ReturnCondition { get; set; }
}
