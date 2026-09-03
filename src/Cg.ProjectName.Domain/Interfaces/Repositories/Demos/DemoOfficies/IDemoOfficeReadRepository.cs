using Cg.ProjectName.Domain.Entities.Demos;

namespace Cg.ProjectName.Domain.Interfaces.Repositories.Demos.DemoOfficies;

/// <summary>
/// Repositório de leitura (consultas), implementado via Dapper.
/// Usado para queries diretas/otimizadas, sem tracking do EF Core.
/// </summary>
public interface IDemoOfficeReadRepository
{
    Task<DemoOffice?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}
