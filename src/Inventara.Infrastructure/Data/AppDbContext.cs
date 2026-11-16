using System.Linq.Expressions;
using Inventara.Domain.Common;
using Inventara.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Inventara.Infrastructure.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Location> Locations => Set<Location>();

    public DbSet<Vendor> Vendors => Set<Vendor>();

    public DbSet<Asset> Assets => Set<Asset>();

    public DbSet<Loan> Loans => Set<Loan>();

    public DbSet<Maintenance> Maintenances => Set<Maintenance>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                     .Where(entityType => typeof(BaseEntity).IsAssignableFrom(entityType.ClrType)))
        {
            var entityParameter = Expression.Parameter(entityType.ClrType, "entity");
            var isDeletedProperty = Expression.Property(entityParameter, nameof(BaseEntity.IsDeleted));
            var filter = Expression.Lambda(
                Expression.Equal(isDeletedProperty, Expression.Constant(false)),
                entityParameter);

            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
        }

        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges()
    {
        UpdateEntityTimestamps();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        UpdateEntityTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateEntityTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        UpdateEntityTimestamps();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void UpdateEntityTimestamps()
    {
        var now = DateTime.UtcNow;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = now;
                entry.Property(entity => entity.CreatedAt).IsModified = false;
            }
        }
    }
}
