using System.Linq.Expressions;
using Inventara.Domain.Common;
using Inventara.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventara.Infrastructure.Data.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261128000000_AddUserSecurityStamp")]
public partial class AddUserSecurityStamp : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "SecurityStamp",
            table: "Users",
            type: "uniqueidentifier",
            nullable: true);

        migrationBuilder.Sql("UPDATE [Users] SET [SecurityStamp] = NEWID() WHERE [SecurityStamp] IS NULL;");

        migrationBuilder.AlterColumn<Guid>(
            name: "SecurityStamp",
            table: "Users",
            type: "uniqueidentifier",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uniqueidentifier",
            oldNullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropColumn(
            name: "SecurityStamp",
            table: "Users");

    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasAnnotation("ProductVersion", "10.0.15")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);
        SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);
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
    }
}
