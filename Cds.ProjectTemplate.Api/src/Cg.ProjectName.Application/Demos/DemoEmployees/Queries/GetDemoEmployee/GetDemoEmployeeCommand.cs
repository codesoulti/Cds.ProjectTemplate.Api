using MediatR;

namespace Cg.ProjectName.Application.Demos.DemoEmployees.Queries.GetDemoEmployee;

public class GetDemoEmployeeCommand 
    : IRequest<GetDemoEmployeeDto>
{
    public Guid Id { get; init; }

    public GetDemoEmployeeCommand(Guid id)
    {
        Id = id;
    }
}