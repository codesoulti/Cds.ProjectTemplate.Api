using Cg.ProjectName.Domain.Options.Dapper;

namespace Cg.ProjectName.Application.Shared.Paginations.Dapper;

public class DapperPagedAndSortedCommand
{
    public SortingOptions? Sorting { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
