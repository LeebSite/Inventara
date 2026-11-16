using Inventara.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventara.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = DatabaseConnectionString.Create(configuration);

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                connectionString,
                sqlServerOptions => sqlServerOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName)));

        return services;
    }
}
