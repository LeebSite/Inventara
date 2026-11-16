using Inventara.Application.Persistence;
using Inventara.Domain.Entities;
using Inventara.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventara.Infrastructure.Persistence;

public sealed class UserRepository : EntityRepository<User>, IUserRepository
{
    private readonly AppDbContext _dbContext;

    public UserRepository(AppDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<User?> GetByEmailAsync(string normalizedEmail, CancellationToken cancellationToken) =>
        _dbContext.Users.FirstOrDefaultAsync(
            user => user.Email.ToLower() == normalizedEmail,
            cancellationToken);

    public Task<bool> EmailExistsAsync(
        string normalizedEmail,
        Guid? exceptUserId,
        CancellationToken cancellationToken) =>
        _dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(
                user => user.Email.ToLower() == normalizedEmail
                    && (exceptUserId == null || user.Id != exceptUserId.Value),
                cancellationToken);
}
