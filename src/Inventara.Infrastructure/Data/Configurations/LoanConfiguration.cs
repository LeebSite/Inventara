using Inventara.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventara.Infrastructure.Data.Configurations;

public sealed class LoanConfiguration : IEntityTypeConfiguration<Loan>
{
    public void Configure(EntityTypeBuilder<Loan> builder)
    {
        builder.Property(loan => loan.Status).HasConversion<string>().HasMaxLength(32).IsRequired();
        builder.Property(loan => loan.Purpose).HasMaxLength(1000).IsRequired();
        builder.Property(loan => loan.RejectionReason).HasMaxLength(1000);
        builder.Property(loan => loan.ReturnCondition).HasConversion<string>().HasMaxLength(32);

        builder.HasOne<Asset>()
            .WithMany()
            .HasForeignKey(loan => loan.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(loan => loan.BorrowerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(loan => loan.ApprovedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
