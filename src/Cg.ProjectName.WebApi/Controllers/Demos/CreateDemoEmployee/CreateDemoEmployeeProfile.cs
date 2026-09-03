using AutoMapper;
using Cg.ProjectName.Application.Demos.DemoEmployees.Commands.CreateDemoEmployee;

namespace Cg.ProjectName.WebApi.Controllers.Demos.CreateDemoEmployee;

/// <summary>
/// Profile for mapping between Application and API CreateDemoEmployee responses
/// </summary>
public class CreateDemoEmployeeProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for CreateDemoEmployee feature
    /// </summary>
    public CreateDemoEmployeeProfile()
    {
        CreateMap<CreateDemoEmployeeRequest, CreateDemoEmployeeCommand>();
    }
}
