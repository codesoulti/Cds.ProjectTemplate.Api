using Cg.ProjectName.Application.IntegrationEvents;
using MassTransit;

namespace Cg.ProjectName.Infrastructure.CrossCutting.Messaging;

/// <summary>
/// Implementação concreta de <see cref="IIntegrationEventPublisher"/> usando
/// o <see cref="IPublishEndpoint"/> do MassTransit. Vive neste projeto
/// dedicado (não em Application/Shared, nem em Infrastructure.CrossCutting.Ioc)
/// porque é o único lugar da solução autorizado a conhecer o
/// MassTransit/RabbitMQ (ver comentário em <see cref="IIntegrationEventPublisher"/>) —
/// separado de Ioc para que a mensageria tenha o mesmo tratamento que
/// Infrastructure.Data já tem: um projeto próprio para a implementação,
/// com Ioc apenas compondo (registrando) o que este projeto expõe.
///
/// Registrado como Scoped (ver RabbitMqConfiguration.AddRabbitMqConfiguration)
/// — uma instância por requisição/escopo do MediatR, para que
/// <see cref="_pending"/> acumule só os eventos daquela requisição.
/// </summary>
public class IntegrationEventPublisher(IPublishEndpoint publishEndpoint) : IIntegrationEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint = publishEndpoint;
    private readonly List<object> _pending = [];

    public void Enqueue<TEvent>(TEvent integrationEvent) where TEvent : class
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        _pending.Add(integrationEvent);
    }

    public async Task DispatchAsync(CancellationToken cancellationToken = default)
    {
        if (_pending.Count == 0)
            return;

        // Copia + limpa antes de publicar: se Publish lançar no meio da
        // lista, não queremos uma nova chamada a DispatchAsync (não deveria
        // acontecer nesta pipeline, mas é defensivo) republicando os itens
        // que já foram enviados com sucesso.
        var eventsToDispatch = _pending.ToArray();
        _pending.Clear();

        foreach (var integrationEvent in eventsToDispatch)
        {
            // Publish(object, ...) usa o tipo de runtime da mensagem para
            // decidir o exchange/routing — necessário aqui porque a fila é
            // heterogênea (List<object>); não temos o TEvent genérico neste
            // ponto.
            await _publishEndpoint.Publish(integrationEvent, integrationEvent.GetType(), cancellationToken);
        }
    }
}
