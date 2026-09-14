using Cg.ProjectName.Domain.Entities.Demos;
using Cg.ProjectName.Infrastructure.Data.Configurations.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cg.ProjectName.Infrastructure.Data.Configurations.DemoEmployees;

public sealed class DemoEmployeeConfiguration
    : EntityAudititedAndSoftDeletableConfiguration<DemoEmployee, Guid>
{
    public override void Configure(EntityTypeBuilder<DemoEmployee> builder)
    {
        base.Configure(builder);

        // Schema alinhado com [Table("Employees", Schema = "Demo")] na
        // entidade — antes, este ToTable() sobrescrevia silenciosamente o
        // schema do atributo com o padrão (dbo).
        builder.ToTable("Employees", schema: "Demo");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Document)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.DateHire)
            .IsRequired();

        builder.Property(x => x.DateTermination)
            .IsRequired(false);

        builder.Property(x => x.Salary)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.OfficeId)
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        builder.HasOne(x => x.Office)
            .WithMany(x => x.Employees)
            .HasForeignKey(x => x.OfficeId)
            .OnDelete(DeleteBehavior.Restrict);

        // Índice único filtrado: soft-delete não pode travar o Document de um
        // funcionário desligado para sempre — sem o filtro, recontratar
        // alguém com o mesmo documento falharia com 409, mesmo o registro
        // antigo estando logicamente excluído.
        builder.HasIndex(x => x.Document)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => x.OfficeId);
        builder.HasIndex(x => x.Status);

        // Cobre o padrão de filtro mais comum da listagem (GetListAsync):
        // OfficeId + Status combinados. Os índices simples acima continuam
        // necessários para filtros isolados por um dos dois campos.
        builder.HasIndex(x => new { x.OfficeId, x.Status });
    }
}