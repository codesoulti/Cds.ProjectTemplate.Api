using Cg.ProjectName.Domain.Enums.Dapper;
using System.Linq.Expressions;

namespace Cg.ProjectName.Domain.Options.Dapper;

public static class DapperSortOptions
{
    public static DapperSortOptions<TEntity> Asc<TEntity>(
        Expression<Func<TEntity, object>> expression)
    {
        return new(
            expression,
            SortDirection.Asc);
    }

    public static DapperSortOptions<TEntity> Desc<TEntity>(
        Expression<Func<TEntity, object>> expression)
    {
        return new(
            expression,
            SortDirection.Desc);
    }
}

public sealed record DapperSortOptions<TEntity>(
    Expression<Func<TEntity, object>> Expression,
    SortDirection Direction);