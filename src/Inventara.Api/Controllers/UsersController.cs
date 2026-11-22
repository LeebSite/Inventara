using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Inventara.Api.Models;
using Inventara.Application.Common;
using Inventara.Application.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventara.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/users")]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ApiResponse<PagedResponse<UserResponse>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResponse<UserResponse>>>> GetPage(
        [FromQuery] ListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await userService.GetPageAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResponse<UserResponse>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ApiResponse<UserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await userService.GetByIdAsync(id, cancellationToken);
        return Ok(ApiResponse<UserResponse>.Ok(result));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<ApiResponse<UserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<UserResponse>>> Update(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await userService.UpdateAsync(id, CurrentUserId, request, cancellationToken);
        return Ok(ApiResponse<UserResponse>.Ok(result));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await userService.DeleteAsync(id, CurrentUserId, cancellationToken);
        return NoContent();
    }

    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId)
            ? userId
            : throw new InvalidOperationException("The access token does not identify a valid user.");
}
