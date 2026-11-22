using System.Text.Json;
using System.Linq.Expressions;
using FluentValidation;
using Inventara.Application.Common;
using Inventara.Application.Persistence;
using Inventara.Domain.Entities;

namespace Inventara.Application.AuditLogs;

public sealed class AuditLogService(
    IEntityRepository<AuditLog> auditLogs,
    IValidator<AuditLogQuery> queryValidator) : IAuditLogService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task AddAsync(
        Guid? userId,
        string entityName,
        Guid entityId,
        string action,
        object? oldValues,
        object? newValues,
        CancellationToken cancellationToken)
    {
        await auditLogs.AddAsync(
            new AuditLog
            {
                UserId = userId,
                EntityName = entityName,
                EntityId = entityId,
                Action = action,
                OldValues = oldValues is null ? null : JsonSerializer.Serialize(oldValues, JsonOptions),
                NewValues = newValues is null ? null : JsonSerializer.Serialize(newValues, JsonOptions)
            },
            cancellationToken);
    }

    public async Task<PagedResponse<AuditLogResponse>> GetPageAsync(
        AuditLogQuery query,
        CancellationToken cancellationToken)
    {
        await queryValidator.ValidateAndThrowAsync(query, cancellationToken);
        var entityName = query.EntityName?.Trim();
        var fromDate = query.FromDate?.UtcDateTime;
        var toDate = query.ToDate?.UtcDateTime;
        Expression<Func<AuditLog, bool>> predicate = log =>
            (string.IsNullOrEmpty(entityName) || log.EntityName == entityName)
            && (!query.UserId.HasValue || log.UserId == query.UserId.Value)
            && (!fromDate.HasValue || log.CreatedAt >= fromDate.Value)
            && (!toDate.HasValue || log.CreatedAt <= toDate.Value);
        var page = await auditLogs.GetPageAsync(
            predicate,
            query.Page,
            query.PageSize,
            nameof(AuditLog.CreatedAt),
            descending: true,
            cancellationToken);

        return ServiceHelpers.MapPage(page, log => new AuditLogResponse(
            log.Id,
            log.UserId,
            log.EntityName,
            log.EntityId,
            log.Action,
            log.OldValues,
            log.NewValues,
            ServiceHelpers.ToUtcOffset(log.CreatedAt)));
    }
}
