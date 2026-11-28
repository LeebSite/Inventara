using System.Linq.Expressions;
using FluentValidation;
using Inventara.Application.AuditLogs;
using Inventara.Application.Common;
using Inventara.Application.Common.Exceptions;
using Inventara.Application.Persistence;
using Inventara.Application.Security;
using Inventara.Domain.Entities;

namespace Inventara.Application.Users;

public sealed class UserService(
    IUserRepository users,
    IPasswordHasher passwordHasher,
    IValidator<ListQuery> listQueryValidator,
    IValidator<UpdateUserRequest> updateValidator,
    IAuditLogService auditLogs) : IUserService
{
    public async Task<PagedResponse<UserResponse>> GetPageAsync(
        ListQuery query,
        CancellationToken cancellationToken)
    {
        await listQueryValidator.ValidateAndThrowAsync(query, cancellationToken);
        var search = query.Search?.Trim();
        Expression<Func<User, bool>> predicate = string.IsNullOrEmpty(search)
            ? user => true
            : user => user.FullName.Contains(search)
                || user.Email.Contains(search)
                || user.Department.Contains(search)
                || user.Phone.Contains(search);
        var sortBy = ServiceHelpers.ResolveSortField(
            query.SortBy,
            nameof(User.FullName),
            ("id", nameof(User.Id)),
            ("fullName", nameof(User.FullName)),
            ("email", nameof(User.Email)),
            ("role", nameof(User.Role)),
            ("department", nameof(User.Department)),
            ("phone", nameof(User.Phone)),
            ("isActive", nameof(User.IsActive)),
            ("createdAt", nameof(User.CreatedAt)));

        var result = await users.GetPageAsync(
            predicate,
            query.Page,
            query.PageSize,
            sortBy,
            query.SortDir.Equals("desc", StringComparison.OrdinalIgnoreCase),
            cancellationToken);

        return ServiceHelpers.MapPage(result, ToResponse);
    }

    public async Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(id, cancellationToken);
        return user is null
            ? throw new EntityNotFoundException("User not found.")
            : ToResponse(user);
    }

    public async Task<UserResponse> UpdateAsync(
        Guid id,
        Guid actorId,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var user = await users.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            throw new EntityNotFoundException("User not found.");
        }
        var oldValues = ToAuditValues(user, passwordChanged: false);

        var email = request.Email.Trim().ToLowerInvariant();
        if (await users.EmailExistsAsync(email, user.Id, cancellationToken))
        {
            throw new ConflictException("A user with this email already exists.");
        }

        user.FullName = request.FullName.Trim();
        user.Email = email;
        user.Role = request.Role;
        user.Department = request.Department.Trim();
        user.Phone = request.Phone.Trim();
        user.IsActive = request.IsActive;
        var passwordChanged = false;
        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = passwordHasher.Hash(request.Password);
            user.SecurityStamp = Guid.NewGuid();
            passwordChanged = true;
        }

        users.Update(user);
        await auditLogs.AddAsync(
            actorId,
            "User",
            user.Id,
            "Update",
            oldValues,
            ToAuditValues(user, passwordChanged),
            cancellationToken);
        await users.SaveChangesAsync(cancellationToken);
        return ToResponse(user);
    }

    public async Task DeleteAsync(Guid id, Guid actorId, CancellationToken cancellationToken)
    {
        var user = await users.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            throw new EntityNotFoundException("User not found.");
        }

        var oldValues = ToAuditValues(user, passwordChanged: false);
        user.IsDeleted = true;
        users.Update(user);
        await auditLogs.AddAsync(
            actorId,
            "User",
            user.Id,
            "Delete",
            oldValues,
            ToAuditValues(user, passwordChanged: false),
            cancellationToken);
        await users.SaveChangesAsync(cancellationToken);
    }

    private static UserResponse ToResponse(User user) =>
        new(
            user.Id,
            user.FullName,
            user.Email,
            user.Role,
            user.Department,
            user.Phone,
            user.IsActive,
            ServiceHelpers.ToUtcOffset(user.CreatedAt));

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
}
