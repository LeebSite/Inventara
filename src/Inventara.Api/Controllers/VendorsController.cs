using Inventara.Api.Models;
using Inventara.Application.Common;
using Inventara.Application.Vendors;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventara.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/vendors")]
public sealed class VendorsController(IVendorService vendorService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ApiResponse<PagedResponse<VendorResponse>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResponse<VendorResponse>>>> GetPage(
        [FromQuery] ListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await vendorService.GetPageAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResponse<VendorResponse>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ApiResponse<VendorResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<VendorResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await vendorService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<VendorResponse>.Ok(result));
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost]
    [ProducesResponseType<ApiResponse<VendorResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<VendorResponse>>> Create(
        [FromBody] CreateVendorRequest request,
        CancellationToken cancellationToken)
    {
        var result = await vendorService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            ApiResponse<VendorResponse>.Ok(result));
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ApiResponse<VendorResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<VendorResponse>>> Update(
        Guid id,
        [FromBody] UpdateVendorRequest request,
        CancellationToken cancellationToken)
    {
        var result = await vendorService.UpdateAsync(id, request, cancellationToken);
        return Ok(ApiResponse<VendorResponse>.Ok(result));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await vendorService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
