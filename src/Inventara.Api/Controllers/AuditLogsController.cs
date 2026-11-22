using Inventara.Api.Models;
using Inventara.Application.AuditLogs;
using Inventara.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventara.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/audit-logs")]
public sealed class AuditLogsController(IAuditLogService auditLogService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ApiResponse<PagedResponse<AuditLogResponse>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PagedResponse<AuditLogResponse>>>> GetPage(
        [FromQuery] AuditLogQuery query,
        CancellationToken cancellationToken)
    {
        var result = await auditLogService.GetPageAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResponse<AuditLogResponse>>.Ok(result));
    }
}
