using Inventara.Infrastructure.Data;
using Inventara.Infrastructure.Persistence;
using Inventara.Infrastructure.Security;
using Inventara.Infrastructure.Storage;
using Inventara.Application.Persistence;
using Inventara.Application.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventara.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string contentRootPath)
    {
        var connectionString = DatabaseConnectionString.Create(configuration);
        var jwtSettings = JwtSettings.FromConfiguration(configuration);

        services.AddSingleton(jwtSettings);
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sqlServerOptions => sqlServerOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));
        services.AddScoped(typeof(IEntityRepository<>), typeof(EntityRepository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<IAssetPhotoStorage>(_ =>
            new FileStorageService(Path.Combine(contentRootPath, "wwwroot")));
        services.AddSingleton<IAssetExcelExporter, ClosedXmlAssetExporter>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<IJwtTokenIssuer, JwtTokenIssuer>();

        return services;
    }
}
