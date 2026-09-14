using Cg.ProjectName.Domain.Entities.Demos;
using Cg.ProjectName.Domain.Interfaces.Repositories.Demos.DemoOfficies;
using Cg.ProjectName.Domain.Options.Dapper;
using Cg.ProjectName.Infrastructure.Data.Contexts.Dapper;
using Cg.ProjectName.Infrastructure.Data.Repositories.Base;

namespace Cg.ProjectName.Infrastructure.Data.Repositories.Demos.DemoOfficies;

/// <summary>
/// Repositório de leitura (Dapper) de <see cref="DemoOffice"/>. Herda de
/// <see cref="DapperRepository{TEntity, TKey}"/> para reaproveitar
/// <c>GetByIdAsync</c>/<c>GetAllAsync</c> e, com eles, o filtro de
/// soft-delete e a resolução de tabela/schema já usados pelo restante do
/// módulo — antes, este repositório usava SQL manual que não aplicava
/// nenhum dos dois.
/// </summary>
public class DemoOfficeReadRepository(IDbConnectionFactory connectionFactory)
    : ReadRepository<DemoOffice, Guid>(connectionFactory),
    IDemoOfficeReadRepository
{
    public async Task<DemoOffice?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        // Name possui índice único (ver DemoOfficeConfiguration), então no
        // máximo uma linha pode corresponder — FirstOrDefault é suficiente,
        // sem o custo extra de uma consulta paginada (que dispararia também
        // uma query de COUNT desnecessária).
        var result = await QueryListAsync(
            new DapperQueryOptions<DemoOffice>
            {
                Where = x => x.Name == name
            },
            cancellationToken);

        return result.FirstOrDefault();
    }
}
