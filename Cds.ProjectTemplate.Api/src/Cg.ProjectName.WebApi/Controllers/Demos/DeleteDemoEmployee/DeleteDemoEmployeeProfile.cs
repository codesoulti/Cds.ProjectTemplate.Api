using AutoMapper;
using Cg.ProjectName.Application.Demos.DemoEmployees.Commands.DeleteDemoEmployee;

namespace Cg.ProjectName.WebApi.Controllers.Demos.DeleteDemoEmployee;

/// <summary>
/// Profile for mapping between Application and API DeleteDemoEmployee responses
/// </summary>
public class DeleteDemoEmployeeProfile : Profile
{
    /// <summary>
    /// Initializes the mappings for DeleteDemoEmployee feature
    /// </summary>
    public DeleteDemoEmployeeProfile()
    {
        CreateMap<DeleteDemoEmployeeRequest, DeleteDemoEmployeeCommand>();
    }
}
