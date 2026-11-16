using Inventara.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventara.Infrastructure.Data.Configurations;

public sealed class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.Property(vendor => vendor.Name).HasMaxLength(200).IsRequired();
        builder.Property(vendor => vendor.ContactPerson).HasMaxLength(200).IsRequired();
        builder.Property(vendor => vendor.Email).HasMaxLength(320).IsRequired();
        builder.Property(vendor => vendor.Phone).HasMaxLength(50).IsRequired();
        builder.Property(vendor => vendor.Address).HasMaxLength(1000).IsRequired();
    }
}
