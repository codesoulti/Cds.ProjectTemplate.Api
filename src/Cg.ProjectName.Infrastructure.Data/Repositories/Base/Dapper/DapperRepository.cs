using Cg.ProjectName.Domain.Entities.Shared;
using Cg.ProjectName.Domain.Interfaces.Repositories;
using Cg.ProjectName.Domain.Options.Dapper;
using Cg.ProjectName.Domain.ValueObjects.Dapper;
using Cg.ProjectName.Infrastructure.Data.Contexts.Dapper;
using Cg.ProjectName.Infrastructure.Data.Repositories.Base.Dapper.Queries;
using Dapper;
using System.Linq.Expressions;

namespace Cg.ProjectName.Infrastructure.Data.Repositories.Base.Dapper;

public abstract class DapperRepository<TEntity, TKey>(
    IDbConnectionFactory connectionFactory)
    : IDapperRepository<TEntity, TKey>
    where TEntity : Entity<TKey>
    where TKey : IEquatable<TKey>
{
    protected readonly IDbConnectionFactory ConnectionFactory =
        connectionFactory;

    protected static DapperMapping<TEntity, TKey> Mapping =>
        DapperMapping<TEntity, TKey>.Instance;

    public virtual async Task<TEntity?> GetByIdAsync(
        TKey id,
        CancellationToken cancellationToken = default)
    {
        return await GetByIdAsync(
            id,
            null,
            cancellationToken);
    }

    public virtual async Task<TEntity?> GetByIdAsync(
        TKey id,
        Expression<Func<TEntity, object>>? columns,
        CancellationToken cancellationToken = default)
    {
        // Usa BuildById (WHERE {Chave} = @Id direto) em vez de expressar o
        // filtro como "entity.Id!.Equals(id)": como TKey é restrito a
        // IEquatable<TKey>, o compilador resolve esse .Equals para
        // IEquatable<TKey>.Equals(TKey) — um MethodCallExpression cujo
        // DeclaringType não é string — e o WhereExpressionBuilder só sabe
        // traduzir métodos de string, lançando NotSupportedException. Ou
        // seja: GetByIdAsync (a operação mais básica do repositório) lançava
        // exceção sempre que era chamado.
        var query = DapperSqlBuilder.BuildById<TEntity, TKey>(
            id,
            columns);

        var command = new CommandDefinition(
            query.Sql,
            query.Parameters,
            cancellationToken: cancellationToken);

        using var connection = ConnectionFactory.CreateConnection();

        return await connection.QueryFirstOrDefaultAsync<TEntity>(
            command);
    }

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await GetAllAsync(
            null,
            cancellationToken);
    }

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(
        Expression<Func<TEntity, object>>? columns,
        CancellationToken cancellationToken = default)
    {
        var options = new DapperQueryOptions<TEntity>
        {
            Columns = columns
        };

        var query = DapperSqlBuilder.Build<TEntity, TKey>(
            options);

        var command = new CommandDefinition(
            query.Sql,
            query.Parameters,
            cancellationToken: cancellationToken);

        using var connection = ConnectionFactory.CreateConnection();

        var result = await connection.QueryAsync<TEntity>(command);

        return result.AsList();
    }

    public virtual async Task<IReadOnlyList<TEntity>> QueryAsync(
        DapperQueryOptions<TEntity> options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var query = DapperSqlBuilder.Build<TEntity, TKey>(
            options);

        var command = new CommandDefinition(
            query.Sql,
            query.Parameters,
            cancellationToken: cancellationToken);

        using var connection = ConnectionFactory.CreateConnection();

        var result = await connection.QueryAsync<TEntity>(command);

        return result.AsList();
    }

    public virtual async Task<DapperPaginatedListVO<TEntity>> QueryPagedAsync(
        DapperQueryOptions<TEntity> options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (!options.Page.HasValue ||
            !options.PageSize.HasValue)
        {
            throw new ArgumentException(
                "Page e PageSize são obrigatórios para uma consulta paginada.",
                nameof(options));
        }

        // Guarda na própria base genérica (além do validator específico de
        // ListDemoEmployee) — Page/PageSize <= 0 viram OFFSET/FETCH NEXT
        // negativos no SQL Server, o que hoje só falharia como uma
        // SqlException não tratada. Qualquer chamador futuro deste método
        // genérico fica protegido, não só o fluxo que já tem validator.
        if (options.Page.Value < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "Page deve ser maior ou igual a 1.");
        }

        if (options.PageSize.Value < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                "PageSize deve ser maior ou igual a 1.");
        }

        var query =
            DapperSqlBuilder.Build<TEntity, TKey>(
                options);

        var count =
            DapperSqlBuilder.BuildCount<TEntity, TKey>(
                options);

        var sql = $"""
            {count.Sql};

            {query.Sql};
            """;

        var parameters = new DynamicParameters(
            count.Parameters);

        foreach (var parameterName in GetParameterNames(query.Parameters))
        {
            if (parameterName.Equals(
                    "Offset",
                    StringComparison.OrdinalIgnoreCase) ||
                parameterName.Equals(
                    "PageSize",
                    StringComparison.OrdinalIgnoreCase))
            {
                parameters.Add(
                    parameterName,
                    query.Parameters.Get<object>(parameterName));
            }
        }

        var command = new CommandDefinition(
            sql,
            parameters,
            cancellationToken: cancellationToken);

        using var connection = ConnectionFactory.CreateConnection();

        using var multiple = await connection.QueryMultipleAsync(command);

        var totalCount =
            await multiple.ReadSingleAsync<int>();

        var items =
            (await multiple.ReadAsync<TEntity>())
            .AsList();

        return new DapperPaginatedListVO<TEntity>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = options.Page.Value,
            PageSize = options.PageSize.Value
        };
    }

    protected static string ResolveSortDirection(string? direction) =>
        string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase)
            ? "DESC"
            : "ASC";

    protected static void AddOptionalFilter<T>(
        List<string> conditions,
        DynamicParameters parameters,
        T? value,
        string condition,
        string parameterName)
        where T : struct
    {
        if (!value.HasValue)
            return;

        conditions.Add(condition);
        parameters.Add(parameterName, value.Value);
    }

    protected static void AddLikeFilter(
        List<string> conditions,
        DynamicParameters parameters,
        string? value,
        string column,
        string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        conditions.Add($"{column} LIKE '%' + @{parameterName} + '%' ESCAPE '\\'");
        parameters.Add(parameterName, DapperSqlBuilder.EscapeLikeValue(value));
    }

    protected async Task<DapperPaginatedListVO<T>> QueryPaginatedAsync<T>(
        string sql,
        DynamicParameters parameters,
        int currentPage,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        parameters.Add(
            "Offset",
            (currentPage - 1) * pageSize);

        parameters.Add(
            "PageSize",
            pageSize);

        var command = new CommandDefinition(
            sql,
            parameters,
            cancellationToken: cancellationToken);

        using var connection = ConnectionFactory.CreateConnection();

        using var multiple =
            await connection.QueryMultipleAsync(command);

        var totalCount =
            await multiple.ReadSingleAsync<int>();

        var items =
            (await multiple.ReadAsync<T>()).AsList();

        return new DapperPaginatedListVO<T>
        {
            Items = items,
            TotalCount = totalCount,
            CurrentPage = currentPage,
            PageSize = pageSize
        };
    }

    private static IEnumerable<string> GetParameterNames(
        DynamicParameters parameters)
    {
        return parameters.ParameterNames;
    }
}