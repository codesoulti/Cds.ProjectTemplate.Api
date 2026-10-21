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

        // Seguro chamar aqui, antes do commit (feito logo depois, em
        // UnitOfWorkBehavior): o Outbox Transacional do MassTransit garante
        // que isso só vira uma linha durável na mesma transação do
        // SaveChanges, e só é de fato entregue ao RabbitMQ depois que a
        // transação commitar — ver comentário em IIntegrationEventPublisher.
        await _integrationEventPublisher.PublishAsync(
            new DemoEmployeeCreatedIntegrationEvent(
                createdEmployee.Id,
                createdEmployee.Name,
                createdEmployee.Document,
                createdEmployee.DateHire,
                createdEmployee.Salary,
                createdEmployee.OfficeId,
                DateTime.UtcNow),
            cancellationToken);

        return _mapper.Map<CreateDemoEmployeeDto>(createdEmployee);
    }
}