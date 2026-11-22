using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Inventara.Api.Models;
using Inventara.Application.Assets;
using Inventara.Application.Common;
using Inventara.Application.Validation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventara.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/assets")]
public sealed class AssetsController(IAssetService assetService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ApiResponse<PagedResponse<AssetResponse>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResponse<AssetResponse>>>> GetPage(
        [FromQuery] AssetListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await assetService.GetPageAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResponse<AssetResponse>>.Ok(result));
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet("export")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Export(
        [FromQuery] AssetListQuery query,
        CancellationToken cancellationToken)
    {
        var content = await assetService.ExportAsync(query, cancellationToken);
        return File(
            content,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"assets-{DateTime.UtcNow:yyyyMMddHHmmss}.xlsx");
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ApiResponse<AssetDetailResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AssetDetailResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await assetService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<AssetDetailResponse>.Ok(result));
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost]
    [ProducesResponseType<ApiResponse<AssetResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<AssetResponse>>> Create(
        [FromBody] CreateAssetRequest request,
        CancellationToken cancellationToken)
    {
        var result = await assetService.CreateAsync(CurrentUserId, request, cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            ApiResponse<AssetResponse>.Ok(result));
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ApiResponse<AssetResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<AssetResponse>>> Update(
        Guid id,
        [FromBody] UpdateAssetRequest request,
        CancellationToken cancellationToken)
    {
        var result = await assetService.UpdateAsync(id, CurrentUserId, request, cancellationToken);
        return Ok(ApiResponse<AssetResponse>.Ok(result));
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await assetService.DeleteAsync(id, CurrentUserId, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost("{id:guid}/photo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(UploadAssetPhotoRequestValidator.MaximumFileSize + 65536)]
    [ProducesResponseType<ApiResponse<AssetResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AssetResponse>>> UploadPhoto(
        Guid id,
        [FromForm, Required] IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();
        var result = await assetService.UploadPhotoAsync(
            id,
            CurrentUserId,
            new UploadAssetPhotoRequest(
                file.FileName,
                file.ContentType,
                file.Length,
                content),
            cancellationToken);
        return Ok(ApiResponse<AssetResponse>.Ok(result));
    }

    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId)
            ? userId
            : throw new InvalidOperationException("The access token does not identify a valid user.");
}
