namespace Cg.ProjectName.Domain.ValueObjects.Dapper;

public sealed class DapperPaginatedListVO<TEntity>
{
    public IReadOnlyList<TEntity> Items { get; init; } = [];

    public int TotalCount { get; init; }

    public int CurrentPage { get; init; }

    public int PageSize { get; init; }

    public int TotalPages => PageSize <= 0
            ? 0
            : (int)Math.Ceiling(
                (double)TotalCount / PageSize);

    public bool HasPreviousPage => CurrentPage > 1;

    public bool HasNextPage => CurrentPage < TotalPages;
}