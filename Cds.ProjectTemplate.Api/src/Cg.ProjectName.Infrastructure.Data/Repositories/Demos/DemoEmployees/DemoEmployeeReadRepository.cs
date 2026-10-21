using Cg.ProjectName.Domain.Entities.Demos;
using Cg.ProjectName.Domain.Enums.Dapper;
using Cg.ProjectName.Domain.Enums.Shared;
using Cg.ProjectName.Domain.Interfaces.Repositories.Demos.DemoEmployees;
using Cg.ProjectName.Domain.Options.Dapper;
using Cg.ProjectName.Domain.ValueObjects.Demos.DemoEmployees;
using Cg.ProjectName.Domain.ValueObjects.Paginations;
using Cg.ProjectName.Infrastructure.Data.Contexts.Dapper;
using Cg.ProjectName.Infrastructure.Data.Repositories.Base;
using Cg.ProjectName.Infrastructure.Data.Repositories.Base.Dapper;
using Cg.ProjectName.Infrastructure.Data.Repositories.Demos.DemoEmployees.Queries;
using Dapper;

namespace Cg.ProjectName.Infrastructure.Data.Repositories.Demos.DemoEmployees;

public class DemoEmployeeReadRepository(IDbConnectionFactory connectionFactory)
    : ReadRepository<DemoEmployee, Guid>(connectionFactory),
    IDemoEmployeeReadRepository
{
    // Consulta escrita à mão(fora do DapperSqlBuilder genérico) porque essa
    // é a exceção que precisa de LEFT JOIN com DemoOffices para trazer
    // OfficeName — o builder genérico é, por design, de uma tabela só(ver
    // Mapping/DapperMapping<TEntity, TKey>). Não há nada de errado nisso: é
    // assim que se resolve esse caso com Dapper — nem toda consulta precisa
    // passar pelo mini - builder de expressão, que existe só para cobrir o
    // caso comum de uma tabela.
    public async Task<PaginatedListResult<DemoEmployeeResult>> GetListPagedAsync(
        string? name,
        Guid? officeId,
        EStatus? status,
        int currentPage,
        int pageSize,
        SortingOptions? sorting,
        CancellationToken cancellationToken = default)
    {
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

        var sql = DemoEmployeeReadQuery.GetListQuery(whereClause, orderByColumn, direction);

        return await PaginatedQueryAsync<DemoEmployeeResult>(
            sql,
            parameters,
            currentPage,
            pageSize,
            cancellationToken);
    }

    public async Task<IReadOnlyList<DemoEmployee>> GetListAsync(
       string? name,
       Guid? officeId,
       EStatus? status,
       CancellationToken cancellationToken = default)
    {
        var options = new DapperQueryOptions<DemoEmployee>
        {
            Columns = x => new
            {
                x.Id,
                x.Name,
                x.Document,
                x.DateHire,
                x.Salary,
                x.Status
            },
            Where = w => w.Status == status,
            OrderBy =
            [
                new(x => x.Name, SortDirection.Asc)
            ]
        };

        return await QueryListAsync(options, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<DemoEmployeeResult?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();

        AddOptionalFilter<Guid>(
             [],
             parameters,
             id,
             $"e.{Mapping.GetColumnName(nameof(DemoEmployee.Id))} = @Id",
             "Id");

        var sql = DemoEmployeeReadQuery.GetByIdQuery();

        return await QueryFirstOrDefaultAsync<DemoEmployeeResult>(
            sql, parameters, cancellationToken);
    }

    // ExecuteNoQueryAsync(...)
    public Task<int> InactivateEmployeeAsync(
        Guid employeeId,
        CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@EmployeeId", employeeId);

        return ExecuteNoQueryAsync(
            "dbo.usp_DemoEmployee_Inactivate",
            parameters,
            cancellationToken);
    }

    // QueryNoQueryAsync<T>(...)
    public Task<IReadOnlyList<DemoEmployee>>
        GetEmployeesByOfficeProcedureAsync(
            Guid officeId,
            CancellationToken cancellationToken = default)
    {
        var parameters = new DynamicParameters();
        parameters.Add("@OfficeId", officeId);

        return ExecuteNoQueryAsync<DemoEmployee>(
            "dbo.usp_DemoEmployee_GetByOffice",
            parameters,
            cancellationToken);
    }

    #region EXAMPLES OF REPOSITORY METHODS

    //// QueryPagedAsync(options)
    //public Task<PaginatedListResult<DemoEmployee>> GetListPagedAsync(
    //    string? name,
    //    Guid? officeId,
    //    EStatus? status,
    //    int currentPage,
    //    int pageSize,
    //    SortingOptions? sorting,
    //    CancellationToken cancellationToken = default)
    //{
    //    Expression<Func<DemoEmployee, bool>>? where = null;

    //    where = where
    //        .WhereIf(
    //            !name.IsNullOrWhiteSpace(),
    //            x => x.Name.Contains(name)
    //        )
    //        .WhereIf(
    //            officeId.HasValue,
    //            x => x.OfficeId == officeId
    //        )
    //        .WhereIf(
    //            status.HasValue,
    //            x => x.Status == status
    //        );

    //    var orderBy = sorting.ToDapperSort(
    //        new Dictionary<string, Expression<Func<DemoEmployee, object>>>
    //        {
    //            ["name"] = x => x.Name,
    //            ["salary"] = x => x.Salary,
    //            ["datehire"] = x => x.DateHire,
    //            ["status"] = x => x.Status
    //        });

    //    var options = new DapperQueryOptions<DemoEmployee>
    //    {
    //        Columns = x => new
    //        {
    //            x.Id,
    //            x.Name,
    //            x.Document,
    //            x.DateHire,
    //            x.Salary,
    //            x.Status
    //        },
    //        Where = where,
    //        OrderBy = [
    //            orderBy
    //        ],
    //        Page = currentPage,
    //        PageSize = pageSize
    //    };

    //    return QueryListPagedAsync<DemoEmployee>(options, cancellationToken);
    //}

    //// GetByIdAsync(id)
    //public Task<DemoEmployee?> GetByIdAsync(
    //    Guid id,
    //    CancellationToken cancellationToken = default)
    //{
    //    return QueryByIdAsync(id, cancellationToken);
    //}

    //// GetByIdAsync(id, columns)
    //public Task<DemoEmployee?> GetEmployeeSummaryByIdAsync(
    //    Guid id,
    //    CancellationToken cancellationToken = default)
    //{
    //    return QueryByIdAsync(
    //        id,
    //        employee => new
    //        {
    //            employee.Id,
    //            employee.Name,
    //            employee.Document,
    //            employee.Status
    //        },
    //        cancellationToken);
    //}

    //// GetAllAsync()
    //public Task<IReadOnlyList<DemoEmployee>> GetAllEmployeesAsync(
    //    CancellationToken cancellationToken = default)
    //{
    //    return QueryAllAsync(cancellationToken);
    //}

    //// GetAllAsync(columns)
    //public Task<IReadOnlyList<DemoEmployee>> GetEmployeeSummariesAsync(
    //    CancellationToken cancellationToken = default)
    //{
    //    return QueryAllAsync(
    //        employee => new
    //        {
    //            employee.Id,
    //            employee.Name,
    //            employee.Status
    //        },
    //        cancellationToken);
    //}

    //// QueryAsync(options)
    //public Task<IReadOnlyList<DemoEmployee>> GetEmployeesByOfficeAsync(
    //    Guid officeId,
    //    CancellationToken cancellationToken = default)
    //{
    //    var options = new DapperQueryOptions<DemoEmployee>
    //    {
    //        // Configure aqui o filtro aceito pelo seu DapperSqlBuilder.
    //        // Exemplo conceitual: OfficeId = officeId.
    //    };

    //    return QueryListAsync(options, cancellationToken);
    //}

    #endregion

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
