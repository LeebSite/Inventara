using Inventara.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventara.Infrastructure.Data.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.Property(auditLog => auditLog.EntityName).HasMaxLength(200).IsRequired();
        builder.Property(auditLog => auditLog.Action).HasMaxLength(100).IsRequired();
        builder.Property(auditLog => auditLog.OldValues).HasColumnType("nvarchar(max)");
        builder.Property(auditLog => auditLog.NewValues).HasColumnType("nvarchar(max)");
        builder.ToTable(table =>
        {
            table.HasCheckConstraint(
                "CK_AuditLogs_OldValues_IsJson",
                "[OldValues] IS NULL OR ISJSON([OldValues]) = 1");
            table.HasCheckConstraint(
                "CK_AuditLogs_NewValues_IsJson",
                "[NewValues] IS NULL OR ISJSON([NewValues]) = 1");
        });

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(auditLog => auditLog.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
