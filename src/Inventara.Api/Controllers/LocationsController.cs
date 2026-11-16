using Inventara.Api.Models;
using Inventara.Application.Common;
using Inventara.Application.Locations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventara.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/locations")]
public sealed class LocationsController(ILocationService locationService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ApiResponse<PagedResponse<LocationResponse>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResponse<LocationResponse>>>> GetPage(
        [FromQuery] ListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await locationService.GetPageAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResponse<LocationResponse>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ApiResponse<LocationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LocationResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await locationService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<LocationResponse>.Ok(result));
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost]
    [ProducesResponseType<ApiResponse<LocationResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<LocationResponse>>> Create(
        [FromBody] CreateLocationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await locationService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            ApiResponse<LocationResponse>.Ok(result));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ApiResponse<LocationResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LocationResponse>>> Update(
        Guid id,
        [FromBody] UpdateLocationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await locationService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<LocationResponse>.Ok(result));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await locationService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
