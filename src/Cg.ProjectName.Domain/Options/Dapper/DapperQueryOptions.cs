using System.Linq.Expressions;

namespace Cg.ProjectName.Domain.Options.Dapper;

public sealed class DapperQueryOptions<TEntity>
{
    public Expression<Func<TEntity, object>>? Columns { get; init; }

    public Expression<Func<TEntity, bool>>? Where { get; init; }

    public IReadOnlyCollection<DapperSortOptions<TEntity>> OrderBy { get; init; } = [];

    //public SortingOptions? Sorting { get; init; }

    public int? Page { get; init; }

    public int? PageSize { get; init; }

    public bool HasPagination => Page.HasValue || PageSize.HasValue;

    public int Offset
    {
        get
        {
            if (!Page.HasValue || !PageSize.HasValue)
                return 0;

            return (Page.Value - 1) * PageSize.Value;
        }
    }
}

public sealed class SortingOptions
{
    public string? Field { get; init; }

    public string? Direction { get; init; }
}