using FluentValidation;
using Inventara.Application.Auth;
using Inventara.Domain.Enums;

namespace Inventara.Application.Validation;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(request => request.Password).NotEmpty().MaximumLength(72);
    }
}

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(request => request.FullName).NotEmpty().MaximumLength(200);
        RuleFor(request => request.Email).NotEmpty().EmailAddress().MaximumLength(320);
        RuleFor(request => request.Password).NotEmpty().MinimumLength(8).MaximumLength(72);
        RuleFor(request => request.Department).NotEmpty().MaximumLength(150);
        RuleFor(request => request.Phone).NotEmpty().MaximumLength(50);
        RuleFor(request => request.Role)
            .Must(role => role is null || Enum.IsDefined(role.Value))
            .WithMessage("Role is invalid.");
    }
}
