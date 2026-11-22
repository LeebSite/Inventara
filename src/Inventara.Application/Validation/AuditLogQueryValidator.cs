using FluentValidation;
using Inventara.Application.AuditLogs;

namespace Inventara.Application.Validation;

public sealed class AuditLogQueryValidator : AbstractValidator<AuditLogQuery>
{
    public AuditLogQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.EntityName).MaximumLength(200);
        RuleFor(query => query)
            .Must(query => !query.FromDate.HasValue
                || !query.ToDate.HasValue
                || query.FromDate <= query.ToDate)
            .WithMessage("FromDate must not be later than ToDate.");
    }
}
