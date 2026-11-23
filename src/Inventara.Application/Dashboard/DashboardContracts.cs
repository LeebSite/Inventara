namespace Inventara.Application.Dashboard;

/// <summary>Aggregate asset and workflow counts for the dashboard.</summary>
public sealed record DashboardSummaryResponse(
    int TotalAssets,
    int AvailableAssets,
    int OnLoanAssets,
    int MaintenanceAssets,
    int RetiredAssets,
    int ActiveLoans,
    int OverdueLoans,
    int ActiveMaintenances);

/// <summary>Asset count for a category chart.</summary>
public sealed record AssetsByCategoryResponse(
    Guid CategoryId,
    string CategoryName,
    int AssetCount);

public sealed record DashboardCategoryCount(
    Guid CategoryId,
    string CategoryName,
    int AssetCount);
