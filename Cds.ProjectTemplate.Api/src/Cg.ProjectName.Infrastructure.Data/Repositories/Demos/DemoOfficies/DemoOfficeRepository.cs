using Cg.ProjectName.Domain.Entities.Demos;
using Cg.ProjectName.Domain.Interfaces.Repositories.Demos.DemoOfficies;
using Cg.ProjectName.Infrastructure.Data.Contexts.EfCore;
using Cg.ProjectName.Infrastructure.Data.Repositories.Base;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Cg.ProjectName.Infrastructure.Data.Repositories.Demos.DemoOfficies;

/// <summary>
/// Initializes a new instance of DemoOfficeWriterRepository
/// </summary>
/// <param name="context">The database context</param>
/// <param name="readRepository">
/// Repositório de leitura (Dapper), usado para resolver/reconsultar um
/// DemoOffice por nome em <see cref="GetOrCreateByNameAsync"/>.
/// </param>
public class DemoOfficeRepository(CgProjectNameDbContext context, IDemoOfficeReadRepository readRepository) :
    Repository<DemoOffice, Guid>(context),
    IDemoOfficeRepository
{
    private readonly IDemoOfficeReadRepository _readRepository = readRepository;

    // Códigos de erro do SQL Server para violação de chave/índice único.
    // 2601: "Cannot insert duplicate key row" (índice único, nosso caso: DemoOffices.Name).
    // 2627: violação de PRIMARY KEY ou UNIQUE CONSTRAINT.
    private const int SqlErrorUniqueIndexViolation = 2601;
    private const int SqlErrorUniqueConstraintViolation = 2627;

    /// <inheritdoc />
    public async Task<DemoOffice> GetOrCreateByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        // O SQL Server ignora espaços à direita em comparações de igualdade,
        // mas não à esquerda — sem o Trim(), " Sales" e "Sales" passariam
        // pelo índice único como nomes distintos, criando duas filiais que
        // deveriam ser a mesma. Trim aqui, no único ponto de entrada que
        // resolve/cria por nome, garante o invariante para todo chamador.
        name = name.Trim();

        var existingOffice = await _readRepository.GetByNameAsync(name, cancellationToken);
        if (existingOffice is not null)
            return existingOffice;

        var newOffice = DemoOffice.Create(name);
        await DbSet.AddAsync(newOffice, cancellationToken);

        try
        {
            // Save isolado, dentro da mesma transação aberta pelo UnitOfWorkBehavior,
            // apenas para detectar o quanto antes uma eventual violação do índice único.
            await Context.SaveChangesAsync(cancellationToken);
            return newOffice;
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            // Outra requisição concorrente criou o mesmo DemoOffice entre a
            // leitura e a inserção acima. Descarta a entidade que falhou e
            // reaproveita a que efetivamente venceu a corrida no banco, em vez
            // de propagar um erro de concorrência para quem chamou.
            Context.Entry(newOffice).State = EntityState.Detached;

            return await _readRepository.GetByNameAsync(name, cancellationToken)
                ?? throw new InvalidOperationException(
                    $"Falha ao resolver o DemoOffice '{name}' após um conflito de concorrência na criação.");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
        => exception.InnerException is SqlException sqlException
           && (sqlException.Number == SqlErrorUniqueIndexViolation
               || sqlException.Number == SqlErrorUniqueConstraintViolation);
}
