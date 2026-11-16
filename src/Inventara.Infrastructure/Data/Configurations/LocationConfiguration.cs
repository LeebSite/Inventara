using Inventara.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventara.Infrastructure.Data.Configurations;

public sealed class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.Property(location => location.Name).HasMaxLength(200).IsRequired();
        builder.Property(location => location.Building).HasMaxLength(150).IsRequired();
        builder.Property(location => location.Floor).HasMaxLength(50).IsRequired();
        builder.Property(location => location.Description).HasMaxLength(1000).IsRequired();
    }
}
