using Cg.ProjectName.Application.Shared.ReadModels.Paginations;
using Cg.ProjectName.Domain.Enums.Shared;
using Cg.ProjectName.Domain.ValueObjects.Paginations;
using MediatR;

namespace Cg.ProjectName.Application.Demos.DemoEmployees.Queries.ListDemoEmployee;

public class ListDemoEmployeeCommand 
    : PagedAndSortedCommand, 
    IRequest<PaginatedListResult<ListDemoEmployeeDto>>
{
    public string? Name { get; set; } = string.Empty;

    public Guid? OfficeId { get; set; }

    public EStatus? Status { get; set; }
}