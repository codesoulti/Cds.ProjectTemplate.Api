using Cg.ProjectName.Infrastructure.Data.Contexts.EfCore;
using Cg.ProjectName.Domain.Entities.Shared;
using Microsoft.EntityFrameworkCore;
using Cg.ProjectName.Domain.Interfaces.Repositories.Base;
using Cg.ProjectName.Domain.Interfaces.Shared;
// IHasRowVersion vem de Cg.ProjectName.Domain.Interfaces.Repositories, já
// coberto pelo using acima.

namespace Cg.ProjectName.Infrastructure.Data.Repositories.Base;

/// <summary>
/// Implementação genérica de <see cref="IRepository{TEntity, TKey}"/> usando EF Core.
/// </summary>
public class Repository<TEntity, TKey>(CgProjectNameDbContext context) : 
    IRepository<TEntity, TKey>
    where TEntity : Entity<TKey>
    where TKey : IEquatable<TKey>
{
    protected readonly CgProjectNameDbContext Context = context;
    protected readonly DbSet<TEntity> DbSet = context.Set<TEntity>();

    public IQueryable<TEntity> Query()
      => DbSet.AsQueryable();

    public IQueryable<TEntity> QueryNoTracking()
      => DbSet.AsQueryable()
        .AsNoTracking();

    public async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
        => await DbSet.FindAsync([id], cancellationToken);

    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await DbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken cancellationToken = default)
        => await DbSet.AddRangeAsync(entities, cancellationToken);

    public async Task UpdateRangeAsync(IEnumerable<TEntity> entities)
        => DbSet.UpdateRange(entities);

    public async Task<TEntity> Update(TEntity entity, byte[]? originalRowVersion = default)
    {
        DbSet.Update(entity);

        // Sem isso, o EF sempre usaria como "valor original" o RowVersion que
        // ele mesmo acabou de ler do banco na mesma requisição (via
        // GetByIdAsync, chamado pelo handler antes de mutar a entidade) — ou
        // seja, a checagem de concorrência otimista nunca detectaria uma
        // edição feita a partir de uma leitura anterior desatualizada
        // (cliente A faz GET, cliente B faz GET + PUT, cliente A faz PUT
        // sobre o dado já obsoleto de A). Configurar explicitamente o valor
        // original a partir do que o CLIENTE enviou de volta é o que fecha
        // essa lacuna.
        if (originalRowVersion is { Length: > 0 } && entity is IHasRowVersion)
        {
            Context.Entry(entity)
                .Property(nameof(IHasRowVersion.RowVersion))
                .OriginalValue = originalRowVersion;
        }

        return entity;
    }

    public async Task Remove(TEntity entity) => DbSet.Remove(entity);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => await Context.SaveChangesAsync(cancellationToken);
}
