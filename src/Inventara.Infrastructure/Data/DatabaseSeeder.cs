using Inventara.Domain.Entities;
using Inventara.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Inventara.Infrastructure.Data;

public static class DatabaseSeeder
{
    private static readonly string[] CategoryNames =
    [
        "Laptop",
        "Monitor",
        "Printer",
        "Kendaraan",
        "Perabot",
        "Jaringan"
    ];

    private static readonly (string Name, string Building, string Floor)[] Locations =
    [
        ("Kantor Pusat Lt.1", "Kantor Pusat", "Lt.1"),
        ("Kantor Pusat Lt.2", "Kantor Pusat", "Lt.2"),
        ("Gudang", "Gudang", string.Empty)
    ];

    public static async Task SeedAsync(
        AppDbContext dbContext,
        IConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var hasAdmin = await dbContext.Users
            .IgnoreQueryFilters()
            .AnyAsync(user => user.Role == Role.Admin, cancellationToken);

        if (!hasAdmin)
        {
            var fullName = configuration["SeedAdmin:FullName"];
            var email = configuration["SeedAdmin:Email"];
            var password = configuration["SeedAdmin:Password"];

            if (string.IsNullOrWhiteSpace(fullName)
                || string.IsNullOrWhiteSpace(email)
                || string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException(
                    "Configure SeedAdmin:FullName, SeedAdmin:Email, and SeedAdmin:Password using User Secrets or environment variables.");
            }

            dbContext.Users.Add(new User
            {
                FullName = fullName,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = Role.Admin,
                IsActive = true
            });
        }

        var existingCategoryNames = await dbContext.Categories
            .IgnoreQueryFilters()
            .Select(category => category.Name)
            .ToListAsync(cancellationToken);
        var existingCategories = new HashSet<string>(existingCategoryNames, StringComparer.OrdinalIgnoreCase);

        foreach (var categoryName in CategoryNames.Where(name => !existingCategories.Contains(name)))
        {
            dbContext.Categories.Add(new Category { Name = categoryName });
        }

        var existingLocations = await dbContext.Locations
            .IgnoreQueryFilters()
            .Select(location => location.Name)
            .ToListAsync(cancellationToken);
        var locationNames = new HashSet<string>(existingLocations, StringComparer.OrdinalIgnoreCase);

        foreach (var location in Locations.Where(location => !locationNames.Contains(location.Name)))
        {
            dbContext.Locations.Add(new Location
            {
                Name = location.Name,
                Building = location.Building,
                Floor = location.Floor
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
