using Cg.ProjectName.Infrastructure.CrossCutting.Ioc;
using Cg.ProjectName.Infrastructure.CrossCutting.Ioc.Configurations;
using Cg.ProjectName.Infrastructure.CrossCutting.Messaging.Configurations;
using Cg.ProjectName.Infrastructure.Data.Contexts.EfCore;
using Cg.ProjectName.Worker.Configurations;
using Cg.ProjectName.Worker.Consumers;
using Hangfire;

var builder = Host.CreateApplicationBuilder(args);

// Registra Infrastructure + Application (EF Core, Dapper, repositórios,
// UnitOfWork, MediatR/pipelines, FluentValidation, jobs) — antes o Worker não
// chamava nada disso (os ModuleInitializers só existiam via
// WebApplicationBuilder), então IDemoEmployeeRepository e companhia
// simplesmente não existiam neste container: o job recorrente do Hangfire
// (demo-employee-maintenance) falhava ao resolver IDemoEmployeeMaintenanceJob
// assim que disparasse. Sem WebApiModuleInitializer (controllers/health
// checks HTTP não fazem sentido aqui) e sem mensageria própria — o Worker
// continua registrando o bus com seus consumers logo abaixo, direto.
DependencyResolver.RegisterWorkerDependencies(builder.Services, builder.Configuration);

builder.Services
    // AddHangfireConfiguration já chama AddHangfireServer() internamente
    // quando Hangfire:Enabled está ligado (ver HangfireConfiguration.cs) —
    // não encadeie outro .AddHangfireServer() aqui: isso registraria um
    // segundo servidor Hangfire rodando dentro do mesmo processo do Worker,
    // ambos concorrendo pela mesma fila de jobs à toa.
    .AddHangfireConfiguration(builder.Configuration)
    // Worker = lado CONSUMIDOR da mensageria (a WebApi só publica — ver
    // WebApiModuleInitializer). AddRabbitMqConfiguration é a mesma extensão
    // usada lá; aqui é chamada diretamente (em vez de por um ModuleInitializer
    // compartilhado, como o resto acima) porque só o Worker precisa passar
    // "configureConsumers" — a WebApi chama a mesma extensão sem esse
    // parâmetro. useOutbox fica no default (false): o Outbox Transacional só
    // faz sentido do lado de quem publica. <CgProjectNameDbContext> aqui é só
    // a restrição genérica do método — não é resolvido/usado em runtime
    // porque useOutbox é false, então não precisa estar registrado neste
    // container para este specific call funcionar (mas está, via
    // RegisterWorkerDependencies acima).
    .AddRabbitMqConfiguration<CgProjectNameDbContext>(builder.Configuration, x =>
    {
        x.AddConsumer<DemoEmployeeCreatedIntegrationEventConsumer>();
    });

var host = builder.Build();

HangfireConfigurationJobs.RegisterRecurringJobs(host.Services);

await host.RunAsync();