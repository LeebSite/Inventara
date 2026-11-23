using Inventara.Api.Models;
using Inventara.Application.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventara.Api.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Staff")]
[Route("api/v1/dashboard")]
public sealed class DashboardController(IDashboardService dashboardService) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType<ApiResponse<DashboardSummaryResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<DashboardSummaryResponse>>> GetSummary(
        CancellationToken cancellationToken)
    {
        var result = await dashboardService.GetSummaryAsync(cancellationToken);
        return Ok(ApiResponse<DashboardSummaryResponse>.Ok(result));
    }

    [HttpGet("assets-by-category")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<AssetsByCategoryResponse>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AssetsByCategoryResponse>>>> GetAssetsByCategory(
        CancellationToken cancellationToken)
    {
        var result = await dashboardService.GetAssetsByCategoryAsync(cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<AssetsByCategoryResponse>>.Ok(result));
    }
}
