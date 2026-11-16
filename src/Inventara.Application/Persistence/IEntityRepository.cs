using System.Linq.Expressions;
using Inventara.Application.Common;
using Inventara.Domain.Common;

namespace Inventara.Application.Persistence;

public interface IEntityRepository<TEntity> where TEntity : BaseEntity
{
    Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<bool> AnyIncludingDeletedAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken);

    Task<PagedResult<TEntity>> GetPageAsync(
        Expression<Func<TEntity, bool>> predicate,
        int page,
        int pageSize,
        string sortBy,
        bool descending,
        CancellationToken cancellationToken);

    Task AddAsync(TEntity entity, CancellationToken cancellationToken);

    void Update(TEntity entity);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
