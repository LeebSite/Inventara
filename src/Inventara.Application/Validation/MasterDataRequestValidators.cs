using FluentValidation;
using Inventara.Application.Categories;
using Inventara.Application.Locations;
using Inventara.Application.Vendors;

namespace Inventara.Application.Validation;

public sealed class CreateCategoryRequestValidator : AbstractValidator<CreateCategoryRequest>
{
    public CreateCategoryRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(150);
        RuleFor(request => request.Description).NotEmpty().MaximumLength(1000);
    }
}

public sealed class UpdateCategoryRequestValidator : AbstractValidator<UpdateCategoryRequest>
{
    public UpdateCategoryRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(150);
        RuleFor(request => request.Description).NotEmpty().MaximumLength(1000);
    }
}

public sealed class CreateLocationRequestValidator : AbstractValidator<CreateLocationRequest>
{
    public CreateLocationRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Building).NotEmpty().MaximumLength(150);
        RuleFor(request => request.Floor).NotEmpty().MaximumLength(50);
        RuleFor(request => request.Description).NotEmpty().MaximumLength(1000);
    }
}

public sealed class UpdateLocationRequestValidator : AbstractValidator<UpdateLocationRequest>
{
    public UpdateLocationRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Building).NotEmpty().MaximumLength(150);
        RuleFor(request => request.Floor).NotEmpty().MaximumLength(50);
        RuleFor(request => request.Description).NotEmpty().MaximumLength(1000);
    }
}

public sealed class CreateVendorRequestValidator : AbstractValidator<CreateVendorRequest>
{
    public CreateVendorRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.ContactPerson).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(request => request.Phone).NotEmpty().MaximumLength(50);
        RuleFor(request => request.Address).NotEmpty().MaximumLength(1000);
    }
}

public sealed class UpdateVendorRequestValidator : AbstractValidator<UpdateVendorRequest>
{
    public UpdateVendorRequestValidator()
    {
        RuleFor(request => request.Name).NotEmpty().MaximumLength(200);
        RuleFor(request => request.ContactPerson).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(request => request.Phone).NotEmpty().MaximumLength(50);
        RuleFor(request => request.Address).NotEmpty().MaximumLength(1000);
    }
}
