using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Inventara.Api.Models;
using Inventara.Application.Common;
using Inventara.Application.Maintenance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventara.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Staff")]
[Route("api/v1/maintenance")]
public sealed class MaintenanceController(IMaintenanceService maintenanceService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ApiResponse<PagedResponse<MaintenanceResponse>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PagedResponse<MaintenanceResponse>>>> GetPage(
        [FromQuery] MaintenanceListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await maintenanceService.GetPageAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResponse<MaintenanceResponse>>.Ok(result));
    }

    [HttpPost]
    [ProducesResponseType<ApiResponse<MaintenanceResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MaintenanceResponse>>> Create(
        [FromBody] CreateMaintenanceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await maintenanceService.CreateAsync(CurrentUserId, request, cancellationToken);
        return CreatedAtAction(
            nameof(GetPage),
            null,
            ApiResponse<MaintenanceResponse>.Ok(result));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<ApiResponse<MaintenanceResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<MaintenanceResponse>>> Update(
        Guid id,
        [FromBody] UpdateMaintenanceRequest request,
        CancellationToken cancellationToken)
    {
        var result = await maintenanceService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<MaintenanceResponse>.Ok(result));
    }

    [HttpPost("{id:guid}/complete")]
    [ProducesResponseType<ApiResponse<MaintenanceResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<MaintenanceResponse>>> Complete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await maintenanceService.CompleteAsync(id, cancellationToken);
        return Ok(ApiResponse<MaintenanceResponse>.Ok(result));
    }

    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId)
            ? userId
            : throw new InvalidOperationException("The access token does not identify a valid user.");
}
