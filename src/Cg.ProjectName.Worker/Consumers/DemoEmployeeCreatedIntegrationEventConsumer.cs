using Cg.ProjectName.Application.IntegrationEvents.Demos;
using MassTransit;

namespace Cg.ProjectName.Worker.Consumers;

/// <summary>
/// Consumer de exemplo para <see cref="DemoEmployeeCreatedIntegrationEvent"/> —
/// mostra a ponta de CONSUMO da mensageria (a WebApi publica, o Worker
/// consome), processo separado do processo web. Registrado em Program.cs via
/// <c>AddRabbitMqConfiguration(..., x => x.AddConsumer&lt;...&gt;())</c>.
///
/// Propositalmente simples (só loga) — troque o corpo por um caso de uso
/// real quando este exemplo virar uma feature de verdade. Se o novo trabalho
/// precisar de acesso a repositórios/DbContext (ex.: gravar algo no banco a
/// partir do evento), injete-os normalmente no construtor: como qualquer
/// consumer MassTransit, cada mensagem é processada num escopo de DI próprio
/// criado pelo bus, então serviços Scoped (IUnitOfWork, repositórios, etc.)
/// funcionam aqui sem cuidado extra.
/// </summary>
public class DemoEmployeeCreatedIntegrationEventConsumer(
    ILogger<DemoEmployeeCreatedIntegrationEventConsumer> logger)
        : IConsumer<DemoEmployeeCreatedIntegrationEvent>
{
    private readonly ILogger<DemoEmployeeCreatedIntegrationEventConsumer> _logger = logger;

    public Task Consume(ConsumeContext<DemoEmployeeCreatedIntegrationEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Funcionário criado recebido via RabbitMQ: {EmployeeId} - {Name} ({Document}), " +
            "escritório {OfficeId}, ocorrido em {OccurredAtUtc:u} (MessageId={MessageId})",
            message.EmployeeId,
            message.Name,
            message.Document,
            message.OfficeId,
            message.OccurredAtUtc,
            context.MessageId);

        // Ponto de extensão: ex. enviar e-mail de boas-vindas, replicar para
        // outro sistema, disparar um relatório, etc. Se a lógica lançar,
        // UseMessageRetry (ver RabbitMqConfiguration) reentrega a mensagem
        // algumas vezes antes de movê-la para a fila
        // "demo-employee-created-integration-event_error".
        return Task.CompletedTask;
    }
}
