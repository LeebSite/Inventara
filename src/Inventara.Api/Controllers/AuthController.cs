using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Inventara.Api.Models;
using Inventara.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventara.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType<ApiResponse<LoginResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.LoginAsync(request, cancellationToken);
        return Ok(ApiResponse<LoginResponse>.Ok(result));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("register")]
    [ProducesResponseType<ApiResponse<UserProfileResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<UserProfileResponse>>> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authService.RegisterAsync(CurrentUserId, request, cancellationToken);
        return CreatedAtAction(
            nameof(UsersController.GetById),
            "Users",
            new { id = result.Id },
            ApiResponse<UserProfileResponse>.Ok(result));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<ApiResponse<UserProfileResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserProfileResponse>>> Me(
        CancellationToken cancellationToken)
    {
        var subject = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (!Guid.TryParse(subject, out var userId))
        {
            return Unauthorized(new ApiProblemDetails
            {
                Type = "https://httpstatuses.com/401",
                Title = "Unauthorized",
                Status = StatusCodes.Status401Unauthorized,
                Detail = "The access token does not identify a valid user.",
                Instance = HttpContext.Request.Path,
                Success = false,
                Message = "The access token does not identify a valid user."
            });
        }

        var result = await authService.GetProfileAsync(userId, cancellationToken);
        return Ok(ApiResponse<UserProfileResponse>.Ok(result));
    }

    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId)
            ? userId
            : throw new InvalidOperationException("The access token does not identify a valid user.");
}
