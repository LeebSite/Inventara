using FluentValidation;
using Inventara.Application.AuditLogs;
using Inventara.Application.Common.Exceptions;
using Inventara.Application.Persistence;
using Inventara.Application.Security;
using Inventara.Application.Users;
using Inventara.Domain.Entities;
using Inventara.Domain.Enums;

namespace Inventara.Application.Auth;

public sealed class AuthService(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IJwtTokenIssuer tokenIssuer,
    IValidator<LoginRequest> loginValidator,
    IValidator<RegisterRequest> registerValidator,
    IAuditLogService auditLogs) : IAuthService
{
    public async Task<LoginResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        await loginValidator.ValidateAndThrowAsync(request, cancellationToken);

        var user = await users.GetByEmailAsync(NormalizeEmail(request.Email), cancellationToken);
        if (user is null
            || !user.IsActive
            || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        var issuedToken = tokenIssuer.Issue(user);
        return new LoginResponse(
            issuedToken.Value,
            "Bearer",
            issuedToken.ExpiresAt,
            ToProfile(user));
    }

    public async Task<UserProfileResponse> RegisterAsync(
        Guid actorId,
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        await registerValidator.ValidateAndThrowAsync(request, cancellationToken);

        var email = NormalizeEmail(request.Email);
        if (await users.EmailExistsAsync(email, null, cancellationToken))
        {
            throw new ConflictException("A user with this email already exists.");
        }

        var user = new User
        {
            FullName = request.FullName.Trim(),
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            Role = request.Role ?? Role.User,
            Department = request.Department.Trim(),
            Phone = request.Phone.Trim(),
            IsActive = true
        };

        await users.AddAsync(user, cancellationToken);
        await auditLogs.AddAsync(
            actorId,
            "User",
            user.Id,
            "Create",
            null,
            ToAuditValues(user, passwordChanged: true),
            cancellationToken);
        await users.SaveChangesAsync(cancellationToken);
        return ToProfile(user);
    }

    public async Task<UserProfileResponse> GetProfileAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            throw new EntityNotFoundException("User not found.");
        }

        return ToProfile(user);
    }

    public async Task LogoutAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(userId, cancellationToken)
            ?? throw new EntityNotFoundException("User not found.");
        user.SecurityStamp = Guid.NewGuid();
        users.Update(user);
        await users.SaveChangesAsync(cancellationToken);
    }

    private static UserProfileResponse ToProfile(User user) =>
        new(
            user.Id,
            user.FullName,
            user.Email,
            user.Role,
            user.Department,
            user.Phone,
            user.IsActive);

    private static UserAuditValues ToAuditValues(User user, bool passwordChanged) =>
        new(
            user.FullName,
            user.Email,
            user.Role.ToString(),
            user.Department,
            user.Phone,
            user.IsActive,
            user.IsDeleted,
            passwordChanged);

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
