using Cg.ProjectName.Domain.Entities.Shared;
using Cg.ProjectName.Domain.Options.Dapper;
using Cg.ProjectName.Domain.ValueObjects.Paginations;
using System.Linq.Expressions;

namespace Cg.ProjectName.Domain.Interfaces.Repositories;

/// <summary>
/// Repositório genérico de leitura (consultas), implementado via Dapper.
/// Usado para queries diretas/otimizadas, sem tracking do EF Core.
/// </summary>
public interface IReadRepository<TEntity, TKey>
    where TEntity : Entity<TKey>
    where TKey : IEquatable<TKey>
{
    Task<TEntity?> QueryByIdAsync(
            TKey id,
            CancellationToken cancellationToken = default);

    Task<TEntity?> QueryByIdAsync(
        TKey id,
        Expression<Func<TEntity, object>>? columns,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Busca todos os registros selecionando TODAS as colunas (SELECT *).
    /// Prefira a sobrecarga com <c>columns</c>, ou <see cref="QueryAsync"/>
    /// com uma projeção explícita — usar esta sobrecarga em código de
    /// produção traz tráfego de rede desnecessário e fica frágil a mudanças
    /// de schema (uma coluna nova/mais pesada passa a ser trazida sem
    /// ninguém pedir).
    /// </summary>
    Task<IReadOnlyList<TEntity>> QueryAllAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> QueryAllAsync(
        Expression<Func<TEntity, object>>? columns,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TEntity>> QueryListAsync(
        DapperQueryOptions<TEntity> options,
        CancellationToken cancellationToken = default);

    Task<PaginatedListResult<T>> QueryListPagedAsync<T>(
        DapperQueryOptions<TEntity> options,
        CancellationToken cancellationToken = default);
}