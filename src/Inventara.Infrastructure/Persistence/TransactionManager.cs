using System.Data;
using Inventara.Application.Persistence;
using Inventara.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventara.Infrastructure.Persistence;

public sealed class TransactionManager(AppDbContext dbContext) : ITransactionManager
{
    public async Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        await operation(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
