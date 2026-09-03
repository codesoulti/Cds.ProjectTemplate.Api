using AutoMapper;
using Cg.ProjectName.Application.Demos.DemoEmployees.Commands.UpdateDemoEmployee;

namespace Cg.ProjectName.WebApi.Controllers.Demos.UpdateDemoEmployee;

/// <summary>
/// Profile for mapping between Application and API UpdateDemoEmployee responses
/// </summary>
public class UpdateDemoEmployeeProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for UpdateDemoEmployee feature
    /// </summary>
    public UpdateDemoEmployeeProfile()
    {
        CreateMap<UpdateDemoEmployeeRequest, UpdateDemoEmployeeCommand>();
    }
}
