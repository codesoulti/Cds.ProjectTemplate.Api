using Cg.ProjectName.Application.IntegrationEvents;
using MassTransit;

namespace Cg.ProjectName.Infrastructure.CrossCutting.Messaging;

/// <summary>
/// Implementação concreta de <see cref="IIntegrationEventPublisher"/> usando
/// o <see cref="IPublishEndpoint"/> do MassTransit. Vive neste projeto
/// dedicado (não em Application/Shared, nem em Infrastructure.CrossCutting.Ioc)
/// porque é o único lugar da solução autorizado a conhecer o
/// MassTransit/RabbitMQ (ver comentário em <see cref="IIntegrationEventPublisher"/>).
///
/// Um passthrough direto para <see cref="IPublishEndpoint.Publish"/> — não
/// acumula nada em memória (isso mudou: ver histórico/README). A garantia de
/// não publicar uma escrita que falhou vem agora do Outbox Transacional do
/// MassTransit (<c>AddEntityFrameworkOutbox</c>/<c>UseBusOutbox</c>, só do
/// lado da WebApi — ver <c>RabbitMqConfiguration</c>), não mais de adiar a
/// publicação manualmente para depois do commit.
///
/// Registrado como Scoped (ver RabbitMqConfiguration.AddRabbitMqConfiguration)
/// — mesma vida do <see cref="IPublishEndpoint"/> injetado.
/// </summary>
public class IntegrationEventPublisher(IPublishEndpoint publishEndpoint) : IIntegrationEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint = publishEndpoint;

    public Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        return _publishEndpoint.Publish(integrationEvent, cancellationToken);
    }
}
