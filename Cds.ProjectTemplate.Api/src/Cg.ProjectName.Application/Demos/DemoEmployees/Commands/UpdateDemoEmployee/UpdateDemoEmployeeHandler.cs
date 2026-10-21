using AutoMapper;
using Cg.ProjectName.Domain.Entities.Demos;
using Cg.ProjectName.Domain.Enums.Shared;
using Cg.ProjectName.Domain.Exceptions;
using Cg.ProjectName.Domain.Interfaces.Repositories.Demos.DemoEmployees;
using Cg.ProjectName.Domain.Interfaces.Repositories.Demos.DemoOfficies;
using MediatR;

namespace Cg.ProjectName.Application.Demos.DemoEmployees.Commands.UpdateDemoEmployee;

public class UpdateDemoEmployeeHandler(
    IDemoEmployeeRepository repository,
    IDemoOfficeRepository officeRepository,
    IMapper mapper)
        : IRequestHandler<UpdateDemoEmployeeCommand, UpdateDemoEmployeeDto>
{
    private readonly IDemoEmployeeRepository _repository = repository;
    private readonly IDemoOfficeRepository _officeRepository = officeRepository;
    private readonly IMapper _mapper = mapper;

    public async Task<UpdateDemoEmployeeDto> Handle(UpdateDemoEmployeeCommand command, CancellationToken cancellationToken)
    {
        var employee = await _repository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw NotFoundException.For<DemoEmployee>(command.Id);

        // GetOrCreateByNameAsync já trata a corrida de duas requisições
        // concorrentes tentando criar o mesmo escritório novo ao mesmo tempo.
        var office = await _officeRepository.GetOrCreateByNameAsync(command.OfficeName, cancellationToken);

        // Muda os dados através do método de domínio (em vez de deixar o
        // AutoMapper sobrescrever campos diretamente), preservando encapsulamento.
        employee.Change(
            command.Name,
            command.Document,
            command.DateHire,
            command.DateTermination,
            command.Salary,
            office.Id
        );

        // Transições de status passam explicitamente pelos métodos de domínio
        // dedicados, em vez de uma atribuição direta ao campo Status.
        if (command.Status == EStatus.Active)
            employee.Activate();
        else
            employee.Inactivate();

        // A persistência efetiva (UPDATE) acontece em UnitOfWorkBehavior,
        // após este handler retornar — não há necessidade de try/catch aqui.
        // command.RowVersion (quando enviado pelo cliente) é o que permite
        // detectar uma edição sobre um dado já desatualizado — ver
        // EfRepository.Update/IHasRowVersion.
        var updatedEmployee = await _repository.Update(employee, command.RowVersion);

        return _mapper.Map<UpdateDemoEmployeeDto>(updatedEmployee);
    }
}