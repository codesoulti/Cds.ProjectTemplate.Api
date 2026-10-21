using Cg.ProjectName.Domain.Options.Dapper;

namespace Cg.ProjectName.WebApi.Shared;

public class PagedAndSortedRequest
{
    public SortingOptions? Sorting { get; set; }
    public int CurrentPage { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}