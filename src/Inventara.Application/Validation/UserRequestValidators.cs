using FluentValidation;
using Inventara.Application.Users;

namespace Inventara.Application.Validation;

public sealed class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(request => request.FullName).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(request => request.Password)
            .MinimumLength(8)
            .MaximumLength(72)
            .When(request => !string.IsNullOrEmpty(request.Password));
        RuleFor(request => request.Role).IsInEnum();
        RuleFor(request => request.Department).NotEmpty().MaximumLength(150);
        RuleFor(request => request.Phone).NotEmpty().MaximumLength(50);
    }
}
