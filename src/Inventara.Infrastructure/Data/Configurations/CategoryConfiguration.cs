using Inventara.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventara.Infrastructure.Data.Configurations;

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.Property(category => category.Name).HasMaxLength(150).IsRequired();
        builder.Property(category => category.Description).HasMaxLength(1000).IsRequired();
        builder.HasIndex(category => category.Name).IsUnique();
    }
}
