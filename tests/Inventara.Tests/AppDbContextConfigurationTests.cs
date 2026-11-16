using Inventara.Domain.Common;
using Inventara.Domain.Entities;
using Inventara.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Inventara.Tests;

public sealed class AppDbContextConfigurationTests
{
    [Fact]
    public void Model_UsesStringEnumsAndConfiguredPrecision()
    {
        using var dbContext = CreateDbContext();

        var assetType = dbContext.Model.FindEntityType(typeof(Asset))!;
        var statusProperty = assetType.FindProperty(nameof(Asset.Status))!;
        var conditionProperty = assetType.FindProperty(nameof(Asset.Condition))!;
        var purchasePriceProperty = assetType.FindProperty(nameof(Asset.PurchasePrice))!;

        Assert.Equal(typeof(string), statusProperty.GetTypeMapping().Converter?.ProviderClrType);
        Assert.Equal(typeof(string), conditionProperty.GetTypeMapping().Converter?.ProviderClrType);
        Assert.Equal(18, purchasePriceProperty.GetPrecision());
        Assert.Equal(2, purchasePriceProperty.GetScale());
    }

    [Fact]
    public void Model_UsesUniqueIndexesAndSoftDeleteFilters()
    {
        using var dbContext = CreateDbContext();

        AssertUniqueIndex<User>(dbContext, nameof(User.Email));
        AssertUniqueIndex<Asset>(dbContext, nameof(Asset.AssetCode));
        AssertUniqueIndex<Asset>(dbContext, nameof(Asset.SerialNumber));
        AssertUniqueIndex<Category>(dbContext, nameof(Category.Name));

        var entityTypes = dbContext.Model.GetEntityTypes()
            .Where(entityType => typeof(BaseEntity).IsAssignableFrom(entityType.ClrType));

        Assert.All(entityTypes, entityType => Assert.NotEmpty(entityType.GetDeclaredQueryFilters()));
    }

    [Fact]
    public void Model_UsesRequestedDeleteBehaviors()
    {
        using var dbContext = CreateDbContext();

        AssertDeleteBehavior<Asset>(dbContext, typeof(Category), DeleteBehavior.Restrict);
        AssertDeleteBehavior<Asset>(dbContext, typeof(Location), DeleteBehavior.Restrict);
        AssertDeleteBehavior<Asset>(dbContext, typeof(Vendor), DeleteBehavior.SetNull);
        AssertDeleteBehavior<Loan>(dbContext, typeof(Asset), DeleteBehavior.Restrict);
        AssertDeleteBehavior<Maintenance>(dbContext, typeof(Asset), DeleteBehavior.Restrict);
        AssertDeleteBehavior<Loan>(dbContext, typeof(User), DeleteBehavior.SetNull, nameof(Loan.ApprovedById));
        AssertDeleteBehavior<AuditLog>(dbContext, typeof(User), DeleteBehavior.SetNull);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=InventaraDb;User Id=sa;TrustServerCertificate=True")
            .Options;

        return new AppDbContext(options);
    }

    private static void AssertUniqueIndex<TEntity>(AppDbContext dbContext, string propertyName)
    {
        var entityType = dbContext.Model.FindEntityType(typeof(TEntity))!;
        var index = entityType.GetIndexes()
            .Single(index => index.Properties.Any(property => property.Name == propertyName));

        Assert.True(index.IsUnique);
    }

    private static void AssertDeleteBehavior<TEntity>(
        AppDbContext dbContext,
        Type principalType,
        DeleteBehavior expectedBehavior,
        string? foreignKeyProperty = null)
    {
        var entityType = dbContext.Model.FindEntityType(typeof(TEntity))!;
        var foreignKey = entityType.GetForeignKeys().Single(key =>
            key.PrincipalEntityType.ClrType == principalType
            && (foreignKeyProperty is null || key.Properties.Any(property => property.Name == foreignKeyProperty)));

        Assert.Equal(expectedBehavior, foreignKey.DeleteBehavior);
    }
}
