using AutoMapper;
using Cg.ProjectName.Domain.Entities.Demos;
using Cg.ProjectName.Domain.Interfaces.Repositories.Demos.DemoEmployees;
using Cg.ProjectName.Domain.Interfaces.Repositories.Demos.DemoOfficies;
using MediatR;

namespace Cg.ProjectName.Application.Demos.DemoEmployees.Commands.CreateDemoEmployee;

public class CreateDemoEmployeeHandler(
    IDemoEmployeeRepository demoEmployeeRepository,
    IDemoOfficeRepository demoOfficeRepository,
    IMapper mapper)
        : IRequestHandler<CreateDemoEmployeeCommand, CreateDemoEmployeeDto>
{
    private readonly IDemoEmployeeRepository _repository = demoEmployeeRepository;
    private readonly IDemoOfficeRepository _officeRepository = demoOfficeRepository;
    private readonly IMapper _mapper = mapper;

    public async Task<CreateDemoEmployeeDto> Handle(CreateDemoEmployeeCommand command, CancellationToken cancellationToken)
    {
        // GetOrCreateByNameAsync já trata a corrida de duas requisições
        // concorrentes tentando criar o mesmo escritório novo ao mesmo tempo.
        var office = await _officeRepository.GetOrCreateByNameAsync(command.OfficeName, cancellationToken);

        var employee = DemoEmployee.Create(
            command.Name,
            command.Document,
            command.DateHire,
            command.Salary,
            office.Id
        );

        // A persistência efetiva (INSERT) acontece em UnitOfWorkBehavior,
        // após este handler retornar — não há necessidade de try/catch aqui.
        var createdEmployee = await _repository.AddAsync(employee, cancellationToken);

        return _mapper.Map<CreateDemoEmployeeDto>(createdEmployee);
    }
}