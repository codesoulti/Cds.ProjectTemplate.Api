using Cg.ProjectName.Domain.Entities.Demos;
using Cg.ProjectName.Domain.Interfaces.Repositories.Base;

namespace Cg.ProjectName.Domain.Interfaces.Repositories.Demos.DemoOfficies;

/// <summary>
/// Repositório de escrita (modificações), implementado via EF Core.
/// Usado para operações de inserção, atualização e exclusão, sem tracking do EF Core.
/// </summary>
public interface IDemoOfficeRepository
    : IRepository<DemoOffice, Guid>
{
    /// <summary>
    /// Busca um <see cref="DemoOffice"/> pelo nome ou cria um novo caso não
    /// exista, de forma segura contra concorrência (duas requisições
    /// simultâneas tentando criar o mesmo escritório novo não resultam em
    /// erro para o usuário nem em registros duplicados).
    /// </summary>
    Task<DemoOffice> GetOrCreateByNameAsync(string name, CancellationToken cancellationToken = default);
}
