using AutoMapper;
using Cg.ProjectName.Domain.Entities.Demos;
using Cg.ProjectName.Domain.ValueObjects.Dapper;
using Cg.ProjectName.Application.Shared.Paginations.Dapper;

namespace Cg.ProjectName.Application.Demos.DemoEmployees.Queries.ListDemoEmployee;

public class ListDemoEmployeeProfile : Profile
{
    public ListDemoEmployeeProfile()
    {
        CreateMap<DemoEmployeeListItem, ListDemoEmployeeDto>();
        CreateMap<DapperPaginatedListVO<DemoEmployeeListItem>, DapperPaginatedListDto<ListDemoEmployeeDto>>();
    }
} 