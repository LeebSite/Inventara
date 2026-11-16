using Inventara.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventara.Infrastructure.Data.Configurations;

public sealed class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.Property(asset => asset.AssetCode).HasMaxLength(50).IsRequired();
        builder.Property(asset => asset.Name).HasMaxLength(200).IsRequired();
        builder.Property(asset => asset.SerialNumber).HasMaxLength(200);
        builder.Property(asset => asset.PurchasePrice).HasPrecision(18, 2).IsRequired();
        builder.Property(asset => asset.Condition).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(asset => asset.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(asset => asset.PhotoUrl).HasMaxLength(2048);
        builder.Property(asset => asset.Notes).HasMaxLength(2000);

        builder.HasIndex(asset => asset.AssetCode).IsUnique();
        builder.HasIndex(asset => asset.SerialNumber)
            .IsUnique()
            .HasFilter("[SerialNumber] IS NOT NULL");

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(asset => asset.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Location>()
            .WithMany()
            .HasForeignKey(asset => asset.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Vendor>()
            .WithMany()
            .HasForeignKey(asset => asset.VendorId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
