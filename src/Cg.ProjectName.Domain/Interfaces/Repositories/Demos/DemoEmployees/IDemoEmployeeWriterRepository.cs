using Cg.ProjectName.Domain.Entities.Demos;

namespace Cg.ProjectName.Domain.Interfaces.Repositories.Demos.DemoEmployees;

/// <summary>
/// Repositório de escrita (modificações), implementado via EF Core.
/// Usado para operações de inserção, atualização e exclusão, sem tracking do EF Core.
/// </summary>
public interface IDemoEmployeeWriterRepository 
    : IEFRepository<DemoEmployee, Guid>
{
}
