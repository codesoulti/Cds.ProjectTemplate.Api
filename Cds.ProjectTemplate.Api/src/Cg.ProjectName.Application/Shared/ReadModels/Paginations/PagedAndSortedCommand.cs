using Cg.ProjectName.Domain.Options.Dapper;

namespace Cg.ProjectName.Application.Shared.ReadModels.Paginations;

public class PagedAndSortedCommand
{
    public SortingOptions? Sorting { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

//public sealed record PagedAndSortedCommand(
//    SortingOptions? Sorting,
//    int CurrentPage,
//    int PageSize
//);
