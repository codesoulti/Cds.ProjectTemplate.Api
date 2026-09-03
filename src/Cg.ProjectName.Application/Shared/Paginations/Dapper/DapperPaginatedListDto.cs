namespace Cg.ProjectName.Application.Shared.Paginations.Dapper;

public sealed class DapperPaginatedListDto<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];

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