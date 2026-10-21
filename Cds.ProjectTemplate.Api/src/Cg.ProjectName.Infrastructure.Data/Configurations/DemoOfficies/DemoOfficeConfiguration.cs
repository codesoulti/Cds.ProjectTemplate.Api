using Cg.ProjectName.Domain.Entities.Demos;
using Cg.ProjectName.Infrastructure.Data.Configurations.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cg.ProjectName.Infrastructure.Data.Configurations.DemoOfficies;

public sealed class DemoOfficeConfiguration
    : EntityAudititedAndSoftDeletableConfiguration<DemoOffice, Guid>
{
    public override void Configure(EntityTypeBuilder<DemoOffice> builder)
    {
        base.Configure(builder);

        // Nome/schema alinhados com [Table("Offices", Schema = "Demo")]
        // na entidade — antes, este ToTable() usava um nome de tabela
        // diferente do atributo e sobrescrevia silenciosamente o schema
        // com o padrão (dbo).
        builder.ToTable("Offices", schema: "Demo");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.RowVersion)
            .IsRowVersion();

        // Índice único filtrado: sem isso, uma filial soft-deleted travaria
        // o nome para sempre — GetOrCreateByNameAsync nunca conseguiria
        // recriar uma filial com o mesmo nome de uma desativada.
        builder.HasIndex(x => x.Name)
            .IsUnique()
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => x.Status);
    }
}