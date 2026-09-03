using Cg.ProjectName.Domain.Entities.Demos;
using Cg.ProjectName.Domain.Enums.Shared;
using Cg.ProjectName.Domain.Interfaces.Repositories.Demos.DemoEmployees;
using Cg.ProjectName.Domain.Options.Dapper;
using Cg.ProjectName.Domain.ValueObjects.Dapper;
using Cg.ProjectName.Infrastructure.Data.Contexts.Dapper;
using Cg.ProjectName.Infrastructure.Data.Repositories.Base.Dapper;
using Dapper;

namespace Cg.ProjectName.Infrastructure.Data.Repositories.Demos.DemoEmployees;

public class DemoEmployeeReadRepository(IDbConnectionFactory connectionFactory)
    : DapperRepository<DemoEmployee, Guid>(connectionFactory),
    IDemoEmployeeReadRepository
{
    // Consulta escrita à mão (fora do DapperSqlBuilder genérico) porque essa
    // é a exceção que precisa de LEFT JOIN com DemoOffices para trazer
    // OfficeName — o builder genérico é, por design, de uma tabela só (ver
    // Mapping/DapperMapping<TEntity,TKey>). Não há nada de errado nisso: é
    // assim que se resolve esse caso com Dapper — nem toda consulta precisa
    // passar pelo mini-builder de expressão, que existe só para cobrir o
    // caso comum de uma tabela.
    public async Task<DapperPaginatedListVO<DemoEmployeeListItem>> GetListAsync(
        string? name,
        Guid? officeId,
        EStatus? status,
        int CurrentPage,
        int PageSize,
        SortingOptions? sorting,
        CancellationToken cancellationToken = default)
    {
        var employeeTable = Mapping.TableName;
        var officeTable = DapperMapping<DemoOffice, Guid>.Instance.TableName;

        var conditions = new List<string>
        {
            $"e.{Mapping.GetColumnName(nameof(DemoEmployee.IsDeleted))} = 0"
        };      

        var parameters = new DynamicParameters();

        AddLikeFilter(
            conditions,
            parameters,
            name,
            $"e.{Mapping.GetColumnName(nameof(DemoEmployee.Name))}",
            "Name");

        AddOptionalFilter(
            conditions,
            parameters,
            officeId,
            $"e.{Mapping.GetColumnName(nameof(DemoEmployee.OfficeId))} = @OfficeId",
            "OfficeId");

        AddOptionalFilter(
            conditions,
            parameters,
            status,
            $"e.{Mapping.GetColumnName(nameof(DemoEmployee.Status))} = @Status",
            "Status");

        var whereClause = string.Join(" AND ", conditions);
        var orderByColumn = ResolveSortColumn(sorting?.Field);
        var direction = ResolveSortDirection(sorting?.Direction);

        var sql = $"""
            SELECT COUNT(1)
            FROM {employeeTable} e
            WHERE {whereClause};

            SELECT
                e.{Mapping.GetColumnName(nameof(DemoEmployee.Id))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.Name))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.Document))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.DateHire))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.DateTermination))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.Salary))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.Status))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.OfficeId))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.RowVersion))},
                o.{DapperMapping<DemoOffice, Guid>.Instance.GetColumnName(nameof(DemoOffice.Name))} AS OfficeName
            FROM {employeeTable} e
            LEFT JOIN {officeTable} o
                ON o.{DapperMapping<DemoOffice, Guid>.Instance.GetColumnName(nameof(DemoOffice.Id))}
                 = e.{Mapping.GetColumnName(nameof(DemoEmployee.OfficeId))}
            WHERE {whereClause}
            ORDER BY {orderByColumn} {direction}
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
        """;

        return await QueryPaginatedAsync<DemoEmployeeListItem>(
            sql,
            parameters,
            CurrentPage,
            PageSize,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DemoEmployeeListItem?> GetDetailByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var employeeTable = Mapping.TableName;
        var officeTable = DapperMapping<DemoOffice, Guid>.Instance.TableName;

        var sql = $"""
            SELECT
                e.{Mapping.GetColumnName(nameof(DemoEmployee.Id))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.Name))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.Document))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.DateHire))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.DateTermination))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.Salary))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.Status))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.OfficeId))},
                e.{Mapping.GetColumnName(nameof(DemoEmployee.RowVersion))},
                o.{DapperMapping<DemoOffice, Guid>.Instance.GetColumnName(nameof(DemoOffice.Name))} AS OfficeName
            FROM {employeeTable} e
            LEFT JOIN {officeTable} o
                ON o.{DapperMapping<DemoOffice, Guid>.Instance.GetColumnName(nameof(DemoOffice.Id))}
                 = e.{Mapping.GetColumnName(nameof(DemoEmployee.OfficeId))}
            WHERE e.{Mapping.GetColumnName(nameof(DemoEmployee.Id))} = @Id
              AND e.{Mapping.GetColumnName(nameof(DemoEmployee.IsDeleted))} = 0
            """;

        var command = new CommandDefinition(
            sql,
            new { Id = id },
            cancellationToken: cancellationToken);

        using var connection = ConnectionFactory.CreateConnection();

        return await connection.QuerySingleOrDefaultAsync<DemoEmployeeListItem>(command);
    }

    // "officename" propositalmente não ordena pela coluna física de
    // DemoEmployee — resolve para a coluna já aliasada da filial (join).
    private static string ResolveSortColumn(string? field)
    {
        return field?.ToLowerInvariant() switch
        {
            "salary" => $"e.{Mapping.GetColumnName(nameof(DemoEmployee.Salary))}",
            "datehire" => $"e.{Mapping.GetColumnName(nameof(DemoEmployee.DateHire))}",
            "status" => $"e.{Mapping.GetColumnName(nameof(DemoEmployee.Status))}",
            "officename" => "o." + DapperMapping<DemoOffice, Guid>.Instance.GetColumnName(nameof(DemoOffice.Name)),
            _ => $"e.{Mapping.GetColumnName(nameof(DemoEmployee.Name))}"
        };
    }
}
