using Cg.ProjectName.Domain.Entities.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cg.ProjectName.Infrastructure.Data.Configurations.Shared;

public abstract class EntityAudititedConfiguration<TEntity, TKey>
    : IEntityTypeConfiguration<TEntity>
    where TEntity : EntityAuditited<TKey>
    where TKey : IEquatable<TKey>
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(x => x.CreatedAt)
            .HasColumnName("CreatedAt")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("UpdatedAt");
    }
}

public abstract class EntityFullAudititedConfiguration<TEntity, TKey>
    : EntityAudititedConfiguration<TEntity, TKey>, IEntityTypeConfiguration<TEntity>
    where TEntity : EntityFullAuditited<TKey>
    where TKey : IEquatable<TKey>
{
    public override void Configure(EntityTypeBuilder<TEntity> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.UserId)
            .HasColumnName("UserId");
    }
}

public abstract class EntitySoftDeletableConfiguration<TEntity, TKey>
    : IEntityTypeConfiguration<TEntity>
    where TEntity : EntitySoftDeletable<TKey>
    where TKey : IEquatable<TKey>
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(x => x.IsDeleted)
            .HasColumnName("IsDeleted")
            .HasDefaultValue(false);

        builder.Property(x => x.DeletedAt)
            .HasColumnName("DeletedAt");

        // O global query filter (WHERE IsDeleted = 0) NÃO é aplicado aqui.
        // Ele é responsabilidade única de SoftDeleteModelBuilderExtensions.
        // ApplySoftDelete, chamado a partir de CgProjectNameDbContext.OnModelCreating
        // para toda entidade EntitySoftDeletable<> via reflection. Antes, as
        // duas abordagens coexistiam fazendo a mesma coisa de forma
        // independente — como ApplySoftDelete roda depois de
        // ApplyConfiguration no OnModelCreating, ele sempre sobrescrevia
        // silenciosamente o que fosse configurado aqui, então qualquer
        // mudança feita só neste método (ex.: um filtro adicional de
        // multi-tenant) seria descartada sem aviso.
    }
}

public abstract class EntityAudititedAndSoftDeletableConfiguration<TEntity, TKey>
    : EntitySoftDeletableConfiguration<TEntity, TKey>, IEntityTypeConfiguration<TEntity>
    where TEntity : EntityAudititedAndSoftDeletable<TKey>
    where TKey : IEquatable<TKey>
{
    public override void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(x => x.CreatedAt)
            .HasColumnName("CreatedAt")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("UpdatedAt");

        //// Token de concorrência otimista: coluna SQL Server "rowversion",
        //// gerada e incrementada pelo próprio banco a cada INSERT/UPDATE.
        //// Aplicado aqui (na base) em vez de em cada configuração de entidade
        //// individual, para cobrir DemoEmployee/DemoOffice e qualquer futura
        //// entidade audited+soft-deletable automaticamente.
        //builder.Property(x => x.RowVersion)
        //    .IsRowVersion();

        base.Configure(builder);
    }
}

public abstract class EntityFullAudititedAndSoftDeletableConfiguration<TEntity, TKey>
    : EntitySoftDeletableConfiguration<TEntity, TKey>, IEntityTypeConfiguration<TEntity>
    where TEntity : EntityFullAudititedAndSoftDeletable<TKey>
    where TKey : IEquatable<TKey>
{
    public override void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.Property(x => x.UserId)
           .HasColumnName("UserId");

        builder.Property(x => x.CreatedAt)
            .HasColumnName("CreatedAt")
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(x => x.UpdatedAt)
            .HasColumnName("UpdatedAt");

        base.Configure(builder);
    }
}