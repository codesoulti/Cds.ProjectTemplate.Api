using Cg.ProjectName.Domain.Interfaces.Repositories;

namespace Cg.ProjectName.Domain.Entities.Shared

{
    public abstract class Entity<T> where T : IEquatable<T>
    {
        public required T Id { get; set; }
    }

    public abstract class EntitySoftDeletable<T>
        : Entity<T>, ISoftDelete where T : IEquatable<T>
    {
        public bool IsDeleted { get; protected set; }

        public DateTime? DeletedAt { get; protected set; }

        public void Delete()
        {
            IsDeleted = true;
            DeletedAt = DateTime.UtcNow;
        }

        public void Restore()
        {
            IsDeleted = false;
            DeletedAt = null;
        }
    }

    public abstract class EntityAuditited<T>
        : Entity<T> where T : IEquatable<T>
    {
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public abstract class EntityFullAuditited<T>
        : EntityAuditited<T> where T : IEquatable<T>
    {
        public required T UserId { get; set; }
    }


    public abstract class EntityAudititedAndSoftDeletable<T>
        : EntitySoftDeletable<T>, IHasRowVersion where T : IEquatable<T>
    {
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Token de concorrência otimista (SQL Server rowversion, ver
        // EntityAudititedAndSoftDeletableConfiguration/migrations). Sem isso,
        // duas edições concorrentes do mesmo registro (ex.: dois PUTs
        // simultâneos em /api/DemoEmployee/{id}) resultam em
        // last-writer-wins silencioso — a segunda escrita sobrescreve a
        // primeira sem nenhum aviso. O próprio EF Core preenche este campo;
        // nenhum código de aplicação deve atribuí-lo manualmente.
        //
        // Implementar IHasRowVersion permite que EfRepository<TEntity,TKey>.Update
        // configure o valor ORIGINAL deste token a partir do que o cliente
        // enviou de volta (ver UpdateDemoEmployeeCommand.RowVersion) — sem
        // isso, o EF sempre compararia contra o valor que ele mesmo acabou de
        // ler na mesma requisição, e a checagem de concorrência nunca
        // pegaria uma edição feita a partir de um GET anterior desatualizado.
        public byte[] RowVersion { get; set; } = [];
    }

    public abstract class EntityFullAudititedAndSoftDeletable<T>
        : EntitySoftDeletable<T> where T : IEquatable<T>
    {
        public required T UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}