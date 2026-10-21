using Cg.ProjectName.Domain.Entities.Demos;
using Cg.ProjectName.Domain.Exceptions;
using Cg.ProjectName.Domain.Interfaces.Repositories.Demos.DemoEmployees;
using MediatR;

namespace Cg.ProjectName.Application.Demos.DemoEmployees.Commands.DeleteDemoEmployee;

public class DeleteDemoEmployeeHandler(
    IDemoEmployeeRepository repository)
        : IRequestHandler<DeleteDemoEmployeeCommand, DeleteDemoEmployeeDto>
{
    private readonly IDemoEmployeeRepository _repository = repository;

    public async Task<DeleteDemoEmployeeDto> Handle(DeleteDemoEmployeeCommand request, CancellationToken cancellationToken)
    {
        var demoEmployee = await _repository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For<DemoEmployee>(request.Id);

        await _repository.Remove(demoEmployee);

        return new DeleteDemoEmployeeDto { Success = true };
    }
}
