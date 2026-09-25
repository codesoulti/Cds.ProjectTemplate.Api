using AutoMapper;
using Cg.ProjectName.Application.IntegrationEvents;
using Cg.ProjectName.Application.IntegrationEvents.Demos;
using Cg.ProjectName.Domain.Entities.Demos;
using Cg.ProjectName.Domain.Interfaces.Repositories.Demos.DemoEmployees;
using Cg.ProjectName.Domain.Interfaces.Repositories.Demos.DemoOfficies;
using MediatR;

namespace Cg.ProjectName.Application.Demos.DemoEmployees.Commands.CreateDemoEmployee;

public class CreateDemoEmployeeHandler(
    IDemoEmployeeRepository demoEmployeeRepository,
    IDemoOfficeRepository demoOfficeRepository,
    IIntegrationEventPublisher integrationEventPublisher,
    IMapper mapper)
        : IRequestHandler<CreateDemoEmployeeCommand, CreateDemoEmployeeDto>
{
    private readonly IDemoEmployeeRepository _repository = demoEmployeeRepository;
    private readonly IDemoOfficeRepository _officeRepository = demoOfficeRepository;
    private readonly IIntegrationEventPublisher _integrationEventPublisher = integrationEventPublisher;
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

        // Só ENFILEIRA — a publicação real no RabbitMQ só acontece depois
        // que UnitOfWorkBehavior commitar a transação com sucesso (ver
        // comentário em IIntegrationEventPublisher). Se o SaveChanges/commit
        // falhar, este evento nunca chega a ser publicado.
        _integrationEventPublisher.Enqueue(new DemoEmployeeCreatedIntegrationEvent(
            createdEmployee.Id,
            createdEmployee.Name,
            createdEmployee.Document,
            createdEmployee.DateHire,
            createdEmployee.Salary,
            createdEmployee.OfficeId,
            DateTime.UtcNow));

        return _mapper.Map<CreateDemoEmployeeDto>(createdEmployee);
    }
}