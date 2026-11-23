using FluentValidation;
using Inventara.Application.Maintenance;
using Inventara.Domain.Enums;

namespace Inventara.Application.Validation;

public sealed class MaintenanceListQueryValidator : AbstractValidator<MaintenanceListQuery>
{
    private static readonly string[] SortFields =
    [
        "id",
        "status",
        "reportedAt",
        "startedAt",
        "finishedAt",
        "cost",
        "createdAt"
    ];

    public MaintenanceListQueryValidator()
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
    }
}

public sealed class CreateMaintenanceRequestValidator : AbstractValidator<CreateMaintenanceRequest>
{
    public CreateMaintenanceRequestValidator()
    {
        RuleFor(request => request.AssetId).NotEmpty();
        RuleFor(request => request.Description).NotEmpty().MaximumLength(2000);
    }
}

public sealed class UpdateMaintenanceRequestValidator : AbstractValidator<UpdateMaintenanceRequest>
{
    public UpdateMaintenanceRequestValidator()
    {
        RuleFor(request => request.Status)
            .IsInEnum()
            .NotEqual(MaintenanceStatus.Done)
            .WithMessage("Use the complete action to finish maintenance.");
        RuleFor(request => request.Cost)
            .GreaterThanOrEqualTo(0)
            .When(request => request.Cost.HasValue);
        RuleFor(request => request.FinishedAt).NotEmpty();
    }
}
