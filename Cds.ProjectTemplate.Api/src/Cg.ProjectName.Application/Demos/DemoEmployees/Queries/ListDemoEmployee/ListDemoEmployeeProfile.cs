using AutoMapper;
using Cg.ProjectName.Domain.ValueObjects.Demos.DemoEmployees;
using Cg.ProjectName.Domain.ValueObjects.Paginations;

namespace Cg.ProjectName.Application.Demos.DemoEmployees.Queries.ListDemoEmployee;

public class ListDemoEmployeeProfile : Profile
{
    public ListDemoEmployeeProfile()
    {
        CreateMap<DemoEmployeeResult, ListDemoEmployeeDto>();
        CreateMap<PaginatedListResult<DemoEmployeeResult>, PaginatedListResult<ListDemoEmployeeDto>>();
    }
} 