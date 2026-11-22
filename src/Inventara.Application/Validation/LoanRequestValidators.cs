using FluentValidation;
using Inventara.Application.Loans;

namespace Inventara.Application.Validation;

public sealed class LoanListQueryValidator : AbstractValidator<LoanListQuery>
{
    private static readonly string[] SortFields =
    [
        "id",
        "requestedAt",
        "loanDate",
        "dueDate",
        "returnDate",
        "status",
        "createdAt"
    ];

    public LoanListQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.Status).IsInEnum();
        RuleFor(query => query.SortDir)
            .Must(direction => direction.Equals("asc", StringComparison.OrdinalIgnoreCase)
                || direction.Equals("desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Sort direction must be 'asc' or 'desc'.");
        RuleFor(query => query.SortBy)
            .Must(sortBy => string.IsNullOrWhiteSpace(sortBy)
                || SortFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
            .WithMessage("The requested sort field is not available.");
        RuleFor(query => query)
            .Must(HaveValidDateRanges)
            .WithMessage("Each date range must have its start at or before its end.");
    }

    private static bool HaveValidDateRanges(LoanListQuery query) =>
        (!query.RequestedFrom.HasValue || !query.RequestedTo.HasValue
            || query.RequestedFrom <= query.RequestedTo)
        && (!query.LoanFrom.HasValue || !query.LoanTo.HasValue
            || query.LoanFrom <= query.LoanTo)
        && (!query.DueFrom.HasValue || !query.DueTo.HasValue
            || query.DueFrom <= query.DueTo);
}

public sealed class CreateLoanRequestValidator : AbstractValidator<CreateLoanRequest>
{
    public CreateLoanRequestValidator()
    {
        RuleFor(request => request.AssetId).NotEmpty();
        RuleFor(request => request.DueDate)
            .Must(date => date >= DateTimeOffset.UtcNow)
            .WithMessage("DueDate must not be in the past.");
        RuleFor(request => request.Purpose).NotEmpty().MaximumLength(1000);
    }
}

public sealed class RejectLoanRequestValidator : AbstractValidator<RejectLoanRequest>
{
    public RejectLoanRequestValidator()
    {
        RuleFor(request => request.Reason).NotEmpty().MaximumLength(1000);
    }
}

public sealed class ReturnLoanRequestValidator : AbstractValidator<ReturnLoanRequest>
{
    public ReturnLoanRequestValidator()
    {
        RuleFor(request => request.ReturnCondition).IsInEnum();
    }
}
