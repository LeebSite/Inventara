using System.Linq.Expressions;
using System.Reflection;
using Inventara.Application.Common;
using Inventara.Application.Common.Exceptions;
using Inventara.Application.Persistence;
using Inventara.Domain.Common;
using Inventara.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Inventara.Infrastructure.Persistence;

public class EntityRepository<TEntity>(AppDbContext dbContext) : IEntityRepository<TEntity>
    where TEntity : BaseEntity
{
    public Task<TEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Set<TEntity>().FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);

    public Task<bool> AnyIncludingDeletedAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken cancellationToken) =>
        dbContext.Set<TEntity>()
            .IgnoreQueryFilters()
            .AnyAsync(predicate, cancellationToken);

    public async Task<PagedResult<TEntity>> GetPageAsync(
        Expression<Func<TEntity, bool>> predicate,
        int page,
        int pageSize,
        string sortBy,
        bool descending,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Set<TEntity>().Where(predicate);
        var totalItems = await query.CountAsync(cancellationToken);
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
        var offset = ((long)page - 1) * pageSize;
        if (offset > int.MaxValue)
        {
            return new PagedResult<TEntity>([], totalItems, totalPages, page, pageSize);
        }

        var orderedQuery = ApplyOrdering(query, sortBy, descending);
        var items = await orderedQuery
            .ThenBy(entity => entity.Id)
            .Skip((int)offset)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TEntity>(items, totalItems, totalPages, page, pageSize);
    }

    public Task AddAsync(TEntity entity, CancellationToken cancellationToken) =>
        dbContext.Set<TEntity>().AddAsync(entity, cancellationToken).AsTask();

    public void Update(TEntity entity) => dbContext.Set<TEntity>().Update(entity);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception)
            when (exception.GetBaseException() is SqlException { Number: 2601 or 2627 })
        {
            throw new ConflictException("A record with the same unique value already exists.");
        }
    }

    private static IOrderedQueryable<TEntity> ApplyOrdering(
        IQueryable<TEntity> query,
        string sortBy,
        bool descending)
    {
        var property = typeof(TEntity).GetProperty(
            sortBy,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        if (property is null || property.GetIndexParameters().Length != 0)
        {
            throw new InvalidOperationException("The requested sort field is not available.");
        }

        var parameter = Expression.Parameter(typeof(TEntity), "entity");
        var selector = Expression.Lambda(Expression.Property(parameter, property), parameter);
        var methodName = descending ? nameof(Queryable.OrderByDescending) : nameof(Queryable.OrderBy);
        var method = typeof(Queryable).GetMethods(BindingFlags.Public | BindingFlags.Static)
            .Single(candidate =>
                candidate.Name == methodName
                && candidate.IsGenericMethodDefinition
                && candidate.GetGenericArguments().Length == 2
                && candidate.GetParameters().Length == 2)
            .MakeGenericMethod(typeof(TEntity), property.PropertyType);
        var orderedExpression = Expression.Call(
            method,
            query.Expression,
            Expression.Quote(selector));

        return (IOrderedQueryable<TEntity>)query.Provider.CreateQuery<TEntity>(orderedExpression);
    }
}
