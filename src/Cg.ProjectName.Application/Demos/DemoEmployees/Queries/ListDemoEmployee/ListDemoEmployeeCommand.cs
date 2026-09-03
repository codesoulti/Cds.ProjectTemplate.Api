using Cg.ProjectName.Application.Shared.Paginations.Dapper;
using Cg.ProjectName.Domain.Enums.Shared;
using MediatR;

namespace Cg.ProjectName.Application.Demos.DemoEmployees.Queries.ListDemoEmployee;

public class ListDemoEmployeeCommand 
    : DapperPagedAndSortedCommand, 
    IRequest<DapperPaginatedListDto<ListDemoEmployeeDto>>
{
    public string? Name { get; set; } = string.Empty;

    public Guid? OfficeId { get; set; }

    public EStatus? Status { get; set; }
}