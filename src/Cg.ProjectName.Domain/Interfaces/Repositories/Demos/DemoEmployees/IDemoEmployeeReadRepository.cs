using Cg.ProjectName.Domain.Entities.Demos;
using Cg.ProjectName.Domain.Enums.Shared;
using Cg.ProjectName.Domain.Options.Dapper;
using Cg.ProjectName.Domain.ValueObjects.Demos.DemoEmployees;
using Cg.ProjectName.Domain.ValueObjects.Paginations;

namespace Cg.ProjectName.Domain.Interfaces.Repositories.Demos.DemoEmployees;

/// <summary>
/// Repositório de leitura (consultas), implementado via Dapper.
/// Usado para queries diretas/otimizadas, sem tracking do EF Core.
/// </summary>
public interface IDemoEmployeeReadRepository
    : IReadRepository<DemoEmployee, Guid>
{
    public Task<PaginatedListResult<DemoEmployeeResult>> GetListPagedAsync(
        string? name,
        Guid? officeId,
        EStatus? status,
        int currentPage,
        int pageSize,
        SortingOptions? sorting,
        CancellationToken cancellationToken = default
    );

    public Task<IReadOnlyList<DemoEmployee>> GetListAsync(
        string? name,
        Guid? officeId,
        EStatus? status,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Busca um único funcionário já com o nome da filial (LEFT JOIN),
    /// mesma ideia de <see cref="GetListAsync"/> mas para uma linha só. O
    /// <see cref="IDapperRepository{TEntity, TKey}.GetByIdAsync"/> genérico
    /// (herdado) não serve aqui: ele é de propósito uma consulta de uma
    /// tabela só e não devolveria <c>OfficeName</c>.
    /// </summary>
    Task<DemoEmployeeResult?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
