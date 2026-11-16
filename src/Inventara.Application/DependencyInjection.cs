using FluentValidation;
using Inventara.Application.Auth;
using Inventara.Application.Categories;
using Inventara.Application.Locations;
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
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IVendorService, VendorService>();
        return services;
    }
}
