using Inventara.Application.Common;

namespace Inventara.Application.Users;

public interface IUserService
{
    Task<PagedResponse<UserResponse>> GetPageAsync(ListQuery query, CancellationToken cancellationToken);

    Task<UserResponse> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest request, CancellationToken cancellationToken);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}
