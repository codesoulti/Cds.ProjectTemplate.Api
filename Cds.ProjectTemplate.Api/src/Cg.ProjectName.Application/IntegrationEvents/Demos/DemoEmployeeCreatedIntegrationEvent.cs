namespace Cg.ProjectName.Application.IntegrationEvents.Demos;

/// <summary>
/// Evento de integração publicado no barramento (RabbitMQ) sempre que um
/// <c>DemoEmployee</c> é criado com sucesso — ver
/// <c>CreateDemoEmployeeHandler</c> (quem o enfileira) e
/// <c>UnitOfWorkBehavior</c> (quem efetivamente o despacha, após o commit).
///
/// É um contrato de INTEGRAÇÃO, não a entidade de domínio: propositalmente
/// enxuto e desacoplado do shape de <c>DemoEmployee</c>, para que mudanças
/// internas na entidade não quebrem consumidores externos (dentro ou fora
/// deste processo) — se um dia a entidade ganhar um campo novo, este
/// contrato só muda quando um consumidor de fato precisar dele.
///
/// Tipo <c>record</c> por imutabilidade (uma mensagem já publicada não deve
/// ser alterada) e igualdade estrutural (útil em testes).
/// </summary>
public record DemoEmployeeCreatedIntegrationEvent(
    Guid EmployeeId,
    string Name,
    string Document,
    DateTime DateHire,
    decimal Salary,
    Guid OfficeId,
    DateTime OccurredAtUtc);
