using Inventara.Domain.Enums;

namespace Inventara.Application.Users;

/// <summary>Safe user fields returned by administrative endpoints.</summary>
public sealed record UserResponse(
    Guid Id,
    string FullName,
    string Email,
    Role Role,
    string Department,
    string Phone,
    bool IsActive,
    DateTimeOffset CreatedAt);

/// <summary>Administrative user profile update; password is optional.</summary>
public sealed record UpdateUserRequest
{
    /// <summary>User's full name.</summary>
    public required string FullName { get; init; }

    /// <summary>Unique account email address.</summary>
    public required string Email { get; init; }

    /// <summary>Replacement password; omit to keep the current password.</summary>
    public string? Password { get; init; }

    /// <summary>User's role.</summary>
    public required Role Role { get; init; }

    /// <summary>User's department.</summary>
    public required string Department { get; init; }

    /// <summary>User's phone number.</summary>
    public required string Phone { get; init; }

    /// <summary>Whether the account can authenticate.</summary>
    public bool IsActive { get; init; }
}
