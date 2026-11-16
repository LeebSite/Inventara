using Inventara.Infrastructure;
using Inventara.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Inventara.Api;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var contentRootPath = File.Exists(Path.Combine(currentDirectory, "appsettings.json"))
            ? currentDirectory
            : Path.GetFullPath(Path.Combine(currentDirectory, "src", "Inventara.Api"));

        if (!File.Exists(Path.Combine(contentRootPath, "appsettings.json")))
        {
            throw new InvalidOperationException(
                $"Could not locate Inventara.Api appsettings.json from '{currentDirectory}'.");
        }

        var configuration = new ConfigurationBuilder()
            .SetBasePath(contentRootPath)
            .AddJsonFile("appsettings.json")
            .AddUserSecrets<global::Program>(optional: true)
            .AddEnvironmentVariables()
            .Build();
        var connectionString = DatabaseConnectionString.Create(configuration, requirePassword: false);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(
                connectionString,
                sqlServerOptions => sqlServerOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options;

        return new AppDbContext(options);
    }
}
