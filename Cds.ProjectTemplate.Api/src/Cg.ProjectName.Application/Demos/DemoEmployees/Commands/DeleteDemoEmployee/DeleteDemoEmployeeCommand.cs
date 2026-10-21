using Cg.ProjectName.Application.Interfaces.Shared;

namespace Cg.ProjectName.Application.Demos.DemoEmployees.Commands.DeleteDemoEmployee;

public record DeleteDemoEmployeeCommand : ICommand<DeleteDemoEmployeeDto>
{
    public Guid Id { get; }

    public DeleteDemoEmployeeCommand(Guid id)
    {
        Id = id;
    }
}
