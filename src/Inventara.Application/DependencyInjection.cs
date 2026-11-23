using FluentValidation;
using Inventara.Application.AuditLogs;
using Inventara.Application.Auth;
using Inventara.Application.Assets;
using Inventara.Application.Categories;
using Inventara.Application.Dashboard;
using Inventara.Application.Locations;
using Inventara.Application.Loans;
using Inventara.Application.Maintenance;
using Inventara.Application.Users;
using Inventara.Application.Vendors;
using Microsoft.Extensions.DependencyInjection;

namespace Inventara.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<IAuthService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAssetService, AssetService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<ILoanService, LoanService>();
        services.AddScoped<IMaintenanceService, MaintenanceService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IVendorService, VendorService>();
        return services;
    }
}
