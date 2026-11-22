using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Inventara.Api.Models;
using Inventara.Application.Common;
using Inventara.Application.Loans;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventara.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/loans")]
public sealed class LoansController(ILoanService loanService) : ControllerBase
{
    [Authorize(Roles = "Admin,Staff,User")]
    [HttpPost]
    [ProducesResponseType<ApiResponse<LoanResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<LoanResponse>>> Create(
        [FromBody] CreateLoanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await loanService.CreateAsync(CurrentUserId, request, cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            ApiResponse<LoanResponse>.Ok(result));
    }

    [HttpGet("my")]
    [ProducesResponseType<ApiResponse<PagedResponse<LoanResponse>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResponse<LoanResponse>>>> GetMyPage(
        [FromQuery] LoanListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await loanService.GetMyPageAsync(CurrentUserId, query, cancellationToken);
        return Ok(ApiResponse<PagedResponse<LoanResponse>>.Ok(result));
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet]
    [ProducesResponseType<ApiResponse<PagedResponse<LoanResponse>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResponse<LoanResponse>>>> GetPage(
        [FromQuery] LoanListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await loanService.GetPageAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResponse<LoanResponse>>.Ok(result));
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpGet("overdue")]
    [ProducesResponseType<ApiResponse<PagedResponse<LoanResponse>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResponse<LoanResponse>>>> GetOverduePage(
        [FromQuery] LoanListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await loanService.GetOverduePageAsync(query, cancellationToken);
        return Ok(ApiResponse<PagedResponse<LoanResponse>>.Ok(result));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ApiResponse<LoanResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LoanResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var canViewAll = User.IsInRole("Admin") || User.IsInRole("Staff");
        var result = await loanService.GetByIdAsync(
            id,
            CurrentUserId,
            canViewAll,
            cancellationToken);
        return Ok(ApiResponse<LoanResponse>.Ok(result));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType<ApiResponse<LoanResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<LoanResponse>>> Approve(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await loanService.ApproveAsync(id, CurrentUserId, cancellationToken);
        return Ok(ApiResponse<LoanResponse>.Ok(result));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType<ApiResponse<LoanResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<LoanResponse>>> Reject(
        Guid id,
        [FromBody] RejectLoanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await loanService.RejectAsync(
            id,
            CurrentUserId,
            request,
            cancellationToken);
        return Ok(ApiResponse<LoanResponse>.Ok(result));
    }

    [Authorize(Roles = "Admin,Staff")]
    [HttpPost("{id:guid}/return")]
    [ProducesResponseType<ApiResponse<LoanResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<LoanResponse>>> Return(
        Guid id,
        [FromBody] ReturnLoanRequest request,
        CancellationToken cancellationToken)
    {
        var result = await loanService.ReturnAsync(
            id,
            CurrentUserId,
            request,
            cancellationToken);
        return Ok(ApiResponse<LoanResponse>.Ok(result));
    }

    private Guid CurrentUserId =>
        Guid.TryParse(User.FindFirstValue(JwtRegisteredClaimNames.Sub), out var userId)
            ? userId
            : throw new InvalidOperationException("The access token does not identify a valid user.");
}
