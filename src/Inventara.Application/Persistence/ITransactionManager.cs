namespace Inventara.Application.Persistence;

public interface ITransactionManager
{
    Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken);
}
