using Cg.ProjectName.Domain.Enums.Shared;
using Cg.ProjectName.Domain.Options.Dapper;
using Cg.ProjectName.WebApi.Shared;

namespace Cg.ProjectName.WebApi.Controllers.Demos.ListDemoEmployee;

/// <summary>
/// Request model for getting demo employees
/// </summary>
public class ListDemoEmployeeRequest : PagedAndSortedRequest
{
    /// <summary>
    /// Gets or sets the demo employee name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Filters by the office the employee belongs to. Backed by the
    /// IX_DemoEmployees_OfficeId_Status composite index — previously this
    /// filter existed all the way down to GetListAsync/SQL but had no way to
    /// be reached from the real API surface.
    /// </summary>
    public Guid? OfficeId { get; set; }

    /// <summary>
    /// Filters by employee status (Active/Inactive).
    /// </summary>
    public EStatus? Status { get; set; }

    public ListDemoEmployeeRequest()
    {
        if (Sorting is null)
        {
            Sorting = new SortingOptions
            {
                Field = "Name",
                Direction = "ASC"
            };
        }
    }
}
