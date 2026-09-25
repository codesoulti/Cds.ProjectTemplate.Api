namespace Cg.ProjectName.Application.IntegrationEvents;

/// <summary>
/// Abstração de publicação de eventos de integração — a Application não
/// referencia MassTransit/RabbitMQ diretamente (regra de dependência da
/// Clean Architecture: camadas internas não conhecem detalhes de
/// infraestrutura), apenas esta interface. A implementação concreta (ver
/// <c>IntegrationEventPublisher</c> em Infrastructure.CrossCutting.Ioc)
/// publica de fato no barramento configurado.
///
/// Segue um padrão "outbox em memória, por requisição": <see cref="Enqueue"/>
/// apenas acumula o evento no escopo da requisição atual (não publica nada
/// ainda), e <see cref="DispatchAsync"/> é quem efetivamente publica —
/// chamado por <c>UnitOfWorkBehavior</c> somente DEPOIS que a transação foi
/// commitada com sucesso. Isso evita o cenário clássico de publicar um
/// evento "Xyz foi criado" e, em seguida, a escrita no banco falhar (ex.:
/// erro transiente esgotando as tentativas do EnableRetryOnFailure,
/// violação de constraint) — o consumidor já teria reagido a algo que nunca
/// existiu.
///
/// Não é um Outbox Pattern completo (não persiste os eventos pendentes em
/// tabela própria) — se o processo cair exatamente entre o commit da
/// transação e o Dispatch, o evento se perde. Para garantias mais fortes
/// (at-least-once mesmo em caso de crash do processo), evolua para um outbox
/// real (tabela + processo separado publicando as linhas pendentes).
/// </summary>
public interface IIntegrationEventPublisher
{
    /// <summary>
    /// Acumula <paramref name="integrationEvent"/> para publicação posterior
    /// via <see cref="DispatchAsync"/>. Não publica nada imediatamente.
    /// </summary>
    void Enqueue<TEvent>(TEvent integrationEvent) where TEvent : class;

    /// <summary>
    /// Publica todos os eventos acumulados desde a última chamada (em
    /// ordem) e limpa a fila. Chamado por <c>UnitOfWorkBehavior</c> logo
    /// após o commit da transação — nunca chame diretamente de um Handler.
    /// </summary>
    Task DispatchAsync(CancellationToken cancellationToken = default);
}
