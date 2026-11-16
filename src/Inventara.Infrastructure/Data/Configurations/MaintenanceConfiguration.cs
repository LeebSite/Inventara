using Inventara.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventara.Infrastructure.Data.Configurations;

public sealed class MaintenanceConfiguration : IEntityTypeConfiguration<Maintenance>
{
    public void Configure(EntityTypeBuilder<Maintenance> builder)
    {
        builder.Property(maintenance => maintenance.Description).HasMaxLength(2000).IsRequired();
        builder.Property(maintenance => maintenance.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(maintenance => maintenance.Cost).HasPrecision(18, 2);

        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(maintenance => maintenance.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(maintenance => maintenance.ReportedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Vendor>()
            .WithMany()
            .HasForeignKey(maintenance => maintenance.VendorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
