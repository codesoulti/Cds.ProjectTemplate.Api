using Cg.ProjectName.Application.Demos.DemoEmployees.Base;
using Cg.ProjectName.Application.Interfaces.Shared;

namespace Cg.ProjectName.Application.Demos.DemoEmployees.Commands.CreateDemoEmployee;

public class CreateDemoEmployeeCommand 
    : DemoEmployeeCommandBase,
    ICommand<CreateDemoEmployeeDto>
{
}