using Inventara.Domain.Enums;

namespace Inventara.Application.Auth;

/// <summary>Credentials used to authenticate an active account.</summary>
public sealed record LoginRequest
{
    /// <summary>Account email address.</summary>
    public required string Email { get; init; }

    /// <summary>Account password.</summary>
    public required string Password { get; init; }
}

/// <summary>Data an administrator supplies when registering an account.</summary>
public sealed record RegisterRequest
{
    /// <summary>User's full name.</summary>
    public required string FullName { get; init; }

    /// <summary>Unique account email address.</summary>
    public required string Email { get; init; }

    /// <summary>Initial password for the account.</summary>
    public required string Password { get; init; }

    /// <summary>User's department.</summary>
    public required string Department { get; init; }

    /// <summary>User's phone number.</summary>
    public required string Phone { get; init; }

    /// <summary>Assigned role; defaults to User when omitted.</summary>
    public Role? Role { get; init; }
}

/// <summary>Public profile fields for an account.</summary>
public sealed record UserProfileResponse(
    Guid Id,
    string FullName,
    string Email,
    Role Role,
    string Department,
    string Phone,
    bool IsActive);

/// <summary>Authentication token and the authenticated user's profile.</summary>
public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt,
    UserProfileResponse User);

/// <summary>Token content returned by the configured token issuer.</summary>
public sealed record IssuedToken(string Value, DateTimeOffset ExpiresAt);
