using AutoMapper;
using Cg.ProjectName.Application.Demos.DemoEmployees.Queries.GetDemoEmployee;

namespace Cg.ProjectName.WebApi.Controllers.Demos.GetDemoEmployee;

/// <summary>
/// Profile for mapping GetDemoEmployee feature requests to commands
/// </summary>
public class GetDemoEmployeeProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for GetDemoEmployee feature
    /// </summary>
    public GetDemoEmployeeProfile()
    {
        CreateMap<Guid, GetDemoEmployeeCommand>()
            .ConstructUsing(id => new GetDemoEmployeeCommand(id));
    }
}
