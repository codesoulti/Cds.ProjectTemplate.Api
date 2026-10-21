namespace Cg.ProjectName.Application.IntegrationEvents;

/// <summary>
/// Abstração de publicação de eventos de integração — a Application não
/// referencia MassTransit/RabbitMQ diretamente (regra de dependência da
/// Clean Architecture: camadas internas não conhecem detalhes de
/// infraestrutura), apenas esta interface. A implementação concreta (ver
/// <c>IntegrationEventPublisher</c> em Infrastructure.CrossCutting.Messaging)
/// publica de fato no barramento configurado.
///
/// Antes desta interface tinha Enqueue/DispatchAsync (um "outbox em
/// memória, por requisição" caseiro): acumulava o evento e só publicava
/// depois que UnitOfWorkBehavior confirmasse o commit da transação — mas
/// não sobrevivia a um crash do processo entre o commit e o dispatch (ver
/// git history / README para o desenho anterior).
///
/// Agora <see cref="PublishAsync{TEvent}"/> é chamado diretamente do
/// Handler (não mais "só enfileira") e delega, por baixo, para
/// <c>IPublishEndpoint.Publish</c> do MassTransit — mas isso continua sendo
/// seguro para publicar ANTES do commit porque a implementação real roda
/// atrás do Outbox Transacional do MassTransit (<c>AddEntityFrameworkOutbox</c>
/// + <c>UseBusOutbox</c>, ver <c>RabbitMqConfiguration</c>): a chamada a
/// Publish só acumula a mensagem no escopo atual; ela só é de fato gravada
/// (na tabela OutboxMessage, na MESMA transação) quando
/// <c>UnitOfWorkBehavior</c> chama <c>SaveChangesAsync</c> logo em seguida —
/// e só é entregue ao RabbitMQ depois que a transação commitar, por um
/// serviço de entrega em background que o próprio MassTransit registra. Ou
/// seja: o Handler continua podendo chamar isso sem try/catch, e uma falha
/// na escrita continua não resultando em mensagem publicada — a garantia
/// não vem mais de "só chame isso depois do commit", vem do outbox em si.
///
/// Mantida como abstração (em vez de o Handler injetar IPublishEndpoint
/// direto) para preservar a regra de dependência: Application continua sem
/// referenciar o pacote MassTransit.
/// </summary>
public interface IIntegrationEventPublisher
{
    /// <summary>
    /// Publica <paramref name="integrationEvent"/>. Seguro para chamar antes
    /// do commit da transação atual — ver comentário da interface.
    /// </summary>
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken cancellationToken = default)
        where TEvent : class;
}
