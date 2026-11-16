using Inventara.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventara.Infrastructure.Data.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(user => user.FullName).HasMaxLength(200).IsRequired();
        builder.Property(user => user.Email).HasMaxLength(320).IsRequired();
        builder.Property(user => user.PasswordHash).HasMaxLength(200).IsRequired();
        builder.Property(user => user.Role).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(user => user.Department).HasMaxLength(150).IsRequired();
        builder.Property(user => user.Phone).HasMaxLength(50).IsRequired();
        builder.HasIndex(user => user.Email).IsUnique();
    }
}
