using Cg.ProjectName.Domain.Options.Dapper;
using System.Linq.Expressions;

namespace Cg.Template.Domain.Extensions.Dapper;

public static class DapperSortOptionsExtensions
{
    public static DapperSortOptions<TEntity> ToDapperSort<TEntity>(
        this SortingOptions? sorting,
        IReadOnlyDictionary<string, Expression<Func<TEntity, object>>> allowedColumns)
    {
        if (sorting is null ||
            string.IsNullOrWhiteSpace(sorting.Field))
        {
            return DapperSortOptions.Asc(
                allowedColumns.First().Value);
        }

        if (!allowedColumns.TryGetValue(
                sorting.Field.ToLowerInvariant(),
                out var expression))
        {
            return DapperSortOptions.Asc(
                allowedColumns.First().Value);
        }

        return sorting.Direction?.ToLowerInvariant() == "desc"
            ? DapperSortOptions.Desc(expression)
            : DapperSortOptions.Asc(expression);
    }
}
