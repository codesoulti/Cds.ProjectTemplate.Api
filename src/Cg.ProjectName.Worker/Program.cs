using Cg.ProjectName.Infrastructure.CrossCutting.Ioc.Configurations;
using Cg.ProjectName.Infrastructure.CrossCutting.Messaging.Configurations;
using Cg.ProjectName.Worker.Configurations;
using Cg.ProjectName.Worker.Consumers;
using Hangfire;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
    .AddHangfireConfiguration(builder.Configuration)
    .AddHangfireServer()
    // Worker = lado CONSUMIDOR da mensageria (a WebApi só publica — ver
    // InfrastructureModuleInitializer). AddRabbitMqConfiguration é a mesma
    // extensão usada lá; aqui é chamada diretamente (em vez de via
    // ModuleInitializer/DependencyResolver, que dependem de
    // WebApplicationBuilder) porque o Worker roda sobre um Generic Host puro.
    .AddRabbitMqConfiguration(builder.Configuration, x =>
    {
        x.AddConsumer<DemoEmployeeCreatedIntegrationEventConsumer>();
    });

var host = builder.Build();

HangfireConfigurationJobs.RegisterRecurringJobs(host.Services);

await host.RunAsync();