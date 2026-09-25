using Cg.ProjectName.Application.IntegrationEvents.Demos;
using Cg.ProjectName.Worker.Consumers;
using MassTransit;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Cg.ProjectName.Test.Worker.Consumers;

// DemoEmployeeCreatedIntegrationEventConsumer é a ponta de consumo do exemplo
// de mensageria (RabbitMQ/MassTransit) — a WebApi publica
// DemoEmployeeCreatedIntegrationEvent (ver CreateDemoEmployeeHandler +
// UnitOfWorkBehavior) e o Worker consome. Este teste usa o ITestHarness em
// memória do próprio MassTransit (AddMassTransitTestHarness) em vez de um
// RabbitMQ real: valida que o Consumer está corretamente registrado/roteado
// e que ele processa a mensagem sem lançar, sem depender de infraestrutura
// externa nem de rede — consistente com o resto da suíte (só testes de
// unidade, ver comentário em Cg.ProjectName.Test.csproj).
public class DemoEmployeeCreatedIntegrationEventConsumerTests
{
    private static async Task<ITestHarness> StartHarnessAsync()
    {
        var services = new ServiceCollection();

        services.AddLogging();

        services.AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<DemoEmployeeCreatedIntegrationEventConsumer>();
        });

        var provider = services.BuildServiceProvider(validateScopes: true);
        var harness = provider.GetRequiredService<ITestHarness>();

        await harness.Start();

        return harness;
    }

    [Fact]
    public async Task Consume_WithValidEvent_ConsumesWithoutFaulting()
    {
        var harness = await StartHarnessAsync();

        try
        {
            var integrationEvent = new DemoEmployeeCreatedIntegrationEvent(
                Guid.NewGuid(),
                "Fernando Jose",
                "12345678900",
                DateTime.UtcNow.AddDays(-1),
                5000m,
                Guid.NewGuid(),
                DateTime.UtcNow);

            await harness.Bus.Publish(integrationEvent);

            // A mensagem chegou de fato ao endpoint publicado no bus...
            Assert.True(await harness.Published.Any<DemoEmployeeCreatedIntegrationEvent>());

            // ...e foi consumida com sucesso pelo Consumer sob teste (se
            // Consume tivesse lançado, não apareceria aqui como consumida —
            // iria para retry e, no limite, para a fila de erro).
            var consumerHarness = harness.GetConsumerHarness<DemoEmployeeCreatedIntegrationEventConsumer>();
            Assert.True(await consumerHarness.Consumed.Any<DemoEmployeeCreatedIntegrationEvent>());
        }
        finally
        {
            await harness.Stop();
        }
    }
}
