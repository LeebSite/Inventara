using Inventara.Domain.Entities;

namespace Inventara.Application.Persistence;

public interface IUserRepository : IEntityRepository<User>
{
    Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken);

    Task<bool> EmailExistsAsync(
        string normalizedEmail,
        Guid? exceptUserId,
        CancellationToken cancellationToken);
}
