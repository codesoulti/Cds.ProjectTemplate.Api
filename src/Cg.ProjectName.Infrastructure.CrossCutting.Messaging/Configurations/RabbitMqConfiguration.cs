using Cg.ProjectName.Application.IntegrationEvents;   // IIntegrationEventPublisher, IntegrationEventPublisher
using MassTransit;                                     // IBusRegistrationConfigurator, AddMassTransit, AddEntityFrameworkOutbox
using Microsoft.EntityFrameworkCore;                   // DbContext — restrição genérica de TDbContext abaixo
using Microsoft.Extensions.Configuration;              // IConfiguration
using Microsoft.Extensions.DependencyInjection;        // IServiceCollection

namespace Cg.ProjectName.Infrastructure.CrossCutting.Messaging.Configurations;

/// <summary>
/// Registra o MassTransit com transporte RabbitMQ. Segue o mesmo padrão de
/// <c>RedisConfiguration</c>/<c>HangfireConfiguration</c> (em
/// Infrastructure.CrossCutting.Ioc): uma única extensão sobre
/// <see cref="IServiceCollection"/> (não sobre <c>WebApplicationBuilder</c>),
/// para poder ser chamada tanto pelo <c>WebApiModuleInitializer</c> (WebApi,
/// via <c>DependencyResolver</c>) quanto diretamente do <c>Program.cs</c> do
/// Worker — que usa um Generic Host puro (<c>Host.CreateApplicationBuilder</c>),
/// sem <c>WebApplicationBuilder</c>.
///
/// Vive num projeto próprio (Infrastructure.CrossCutting.Messaging), não em
/// Ioc, porque mensageria tende a crescer bem além de "uma extensão de
/// configuração" — novos eventos, novos consumers, políticas de retry mais
/// elaboradas — e merece um dono próprio, com seus próprios pacotes
/// (MassTransit/MassTransit.RabbitMQ/MassTransit.EntityFrameworkCore)
/// isolados do resto do que Ioc referencia. Mesmo raciocínio que já
/// justifica Infrastructure.Data ser um projeto separado de Ioc.
///
/// <typeparamref name="TDbContext"/> é só uma restrição genérica
/// (<c>where TDbContext : DbContext</c>) — usada exclusivamente para o
/// Outbox Transacional (<paramref name="useOutbox"/>). Este projeto
/// continua sem ProjectReference para Infrastructure.Data por causa disso:
/// não precisa conhecer o tipo concreto do DbContext, só o tipo abstrato do
/// EF Core. Quem sabe qual DbContext concreto passar aqui é quem chama
/// (WebApiModuleInitializer, Worker/Program.cs — ambos já referenciam
/// Infrastructure.Data via Ioc).
///
/// Liga/desliga via <c>RabbitMQ:Enabled</c> (mesma convenção de
/// <c>Redis:Enabled</c>/<c>Hangfire:Enabled</c>): quando desabilitado, NENHUM
/// serviço de mensageria é registrado — inclusive
/// <see cref="IIntegrationEventPublisher"/> fica sem implementação. Diferente
/// do Redis (que tem fallback em memória), aqui não há um "no-op" registrado
/// por padrão: um Handler que dependa de <see cref="IIntegrationEventPublisher"/>
/// falharia a resolver a dependência se RabbitMQ estiver desligado. Isso é
/// deliberado para este exemplo (deixa o "desligado" bem visível em vez de
/// mascarar silenciosamente que nenhum evento está sendo publicado) — troque
/// por um publisher no-op caso prefira que a aplicação suba normalmente sem
/// broker configurado.
/// </summary>
public static class RabbitMqConfiguration
{
    public static bool IsEnabled(IConfiguration configuration) =>
        configuration.GetValue("RabbitMQ:Enabled", defaultValue: true);

    /// <summary>
    /// Registra o barramento MassTransit/RabbitMQ.
    /// </summary>
    /// <typeparam name="TDbContext">
    /// DbContext do lado de escrita — só é usado quando
    /// <paramref name="useOutbox"/> é true (ver comentário da classe).
    /// </typeparam>
    /// <param name="configureConsumers">
    /// Callback para registrar consumers (ex.: <c>x.AddConsumer&lt;MeuConsumer&gt;()</c>)
    /// — chamado ANTES de <c>UsingRabbitMq</c>, como o MassTransit exige. A
    /// WebApi (publisher) não passa nada aqui; o Worker (consumer) registra
    /// os seus. Ver <c>DemoEmployeeCreatedIntegrationEventConsumer</c>.
    /// </param>
    /// <param name="useOutbox">
    /// Liga o Outbox Transacional (pacote MassTransit.EntityFrameworkCore):
    /// mensagens publicadas via <see cref="IIntegrationEventPublisher"/>
    /// dentro do escopo de uma requisição passam a ser gravadas na tabela
    /// OutboxMessage como parte da MESMA transação do SaveChanges de
    /// <typeparamref name="TDbContext"/>, em vez de irem direto pro broker —
    /// sobrevivem a um crash do processo entre o commit e a entrega (ver
    /// README, seção Mensageria). Só a WebApi (publisher) liga isso; o
    /// Worker (consumer) não precisa — passa false (o default).
    ///
    /// Pré-requisito: <typeparamref name="TDbContext"/>.OnModelCreating
    /// precisa chamar modelBuilder.AddInboxStateEntity()/AddOutboxMessageEntity()/
    /// AddOutboxStateEntity() (ver CgProjectNameDbContext), e uma migration
    /// EF Core criando essas tabelas precisa ter sido aplicada — não gerada
    /// automaticamente por este método.
    /// </param>
    public static IServiceCollection AddRabbitMqConfiguration<TDbContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<IBusRegistrationConfigurator>? configureConsumers = null,
        bool useOutbox = false)
        where TDbContext : DbContext
    {
        if (!IsEnabled(configuration))
            return services;

        services.AddScoped<IIntegrationEventPublisher, IntegrationEventPublisher>();

        services.AddMassTransit(busConfigurator =>
        {
            configureConsumers?.Invoke(busConfigurator);

            if (useOutbox)
            {
                // QueryDelay = intervalo de polling do serviço de entrega
                // (hosted service que o próprio AddMassTransit registra) na
                // tabela OutboxMessage. Configurável porque é um trade-off
                // direto entre latência de entrega e carga extra no SQL
                // Server — mesmo raciocínio de deixar Enabled/Host
                // configuráveis por ambiente.
                var queryDelaySeconds = configuration.GetValue("RabbitMQ:OutboxQueryDelaySeconds", defaultValue: 1);

                busConfigurator.AddEntityFrameworkOutbox<TDbContext>(outbox =>
                {
                    outbox.QueryDelay = TimeSpan.FromSeconds(queryDelaySeconds);

                    // Combinado com EnableRetryOnFailure já ligado no mesmo
                    // DbContext (ver InfrastructureModuleInitializer) — o
                    // outbox usa o mesmo provedor/estratégia de conexão.
                    outbox.UseSqlServer();

                    // Faz IPublishEndpoint/ISendEndpoint (injetados em
                    // IntegrationEventPublisher) gravarem na tabela de outbox
                    // em vez de publicarem direto no bus — é o que torna a
                    // publicação parte da transação do SaveChanges.
                    outbox.UseBusOutbox();
                });
            }

            // Nomes de fila previsíveis a partir do nome do Consumer/evento
            // (ex.: "demo-employee-created-integration-event"), em vez do
            // formatador default (que inclui o namespace completo) — mais
            // fácil de reconhecer no management UI do RabbitMQ.
            busConfigurator.SetKebabCaseEndpointNameFormatter();

            busConfigurator.UsingRabbitMq((context, rabbitMqConfigurator) =>
            {
                rabbitMqConfigurator.Host(
                    configuration["RabbitMQ:Host"] ?? "localhost",
                    configuration["RabbitMQ:VirtualHost"] ?? "/",
                    host =>
                    {
                        host.Username(configuration["RabbitMQ:Username"] ?? "guest");
                        host.Password(configuration["RabbitMQ:Password"] ?? "guest");
                    });

                // Retry simples de mensagem (não confundir com o retry de
                // transação do EF Core em UnitOfWorkBehavior — este é sobre
                // reprocessar a mensagem se o Consumer lançar, ex.: uma
                // indisponibilidade momentânea de algo que o Consumer chama).
                // Sem isso, qualquer exceção no Consumer manda a mensagem
                // direto pra fila _error na primeira tentativa.
                rabbitMqConfigurator.UseMessageRetry(retry =>
                    retry.Interval(3, TimeSpan.FromSeconds(5)));

                // Cria os endpoints (filas + bindings) a partir dos
                // consumers registrados acima — precisa vir por último,
                // depois de Host/UseMessageRetry já configurados.
                rabbitMqConfigurator.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
