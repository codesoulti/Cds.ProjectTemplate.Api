using Cg.ProjectName.Domain.Entities.Shared;
using Cg.ProjectName.Domain.Interfaces.Shared;

namespace Cg.ProjectName.Domain.Interfaces.Repositories.Base;

/// <summary>
/// Repositório genérico de escrita (comandos), implementado via EF Core.
/// Responsável por rastrear mudanças e persistir agregados através do DbContext.
/// </summary>
public interface IRepository<TEntity, TKey>
    where TEntity : Entity<TKey>
    where TKey : IEquatable<TKey>
{

    public IQueryable<TEntity> Query();

    public IQueryable<TEntity> QueryNoTracking();

    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);

    // Assinatura sem '?': AddAsync/Update sempre retornam a entidade recebida
    // (nunca null) — antes o Task<TEntity?> era enganoso, obrigando quem
    // chamava a tratar um caso de null que nunca acontecia de fato.
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marca a entidade como modificada. Quando <paramref name="originalRowVersion"/>
    /// é informado e a entidade implementa <see cref="IHasRowVersion"/>, o
    /// valor original do token de concorrência é configurado a partir dele
    /// (em vez do que foi lido do banco na mesma requisição) — é essa
    /// comparação que detecta uma edição feita sobre um dado já
    /// desatualizado (409 Conflict via DbUpdateConcurrencyException, ver
    /// ValidationExceptionMiddleware) em vez de um silencioso last-writer-wins.
    /// </summary>
    Task<TEntity> Update(TEntity entity, byte[]? originalRowVersion = default);

    Task Remove(TEntity entity);

    /// <summary>
    /// Confirma as alterações pendentes no contexto (Unit of Work).
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
