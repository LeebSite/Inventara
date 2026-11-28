using Inventara.Application.Users;

namespace Inventara.Application.Auth;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<UserProfileResponse> RegisterAsync(
        Guid actorId,
        RegisterRequest request,
        CancellationToken cancellationToken);

    Task<UserProfileResponse> GetProfileAsync(Guid userId, CancellationToken cancellationToken);

    Task LogoutAsync(Guid userId, CancellationToken cancellationToken);
}
