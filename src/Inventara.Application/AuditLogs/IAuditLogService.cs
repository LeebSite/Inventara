using Inventara.Application.Common;

namespace Inventara.Application.AuditLogs;

public interface IAuditLogService
{
    Task AddAsync(
        Guid? userId,
        string entityName,
        Guid entityId,
        string action,
        object? oldValues,
        object? newValues,
        CancellationToken cancellationToken);

    Task<PagedResponse<AuditLogResponse>> GetPageAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken);
}
