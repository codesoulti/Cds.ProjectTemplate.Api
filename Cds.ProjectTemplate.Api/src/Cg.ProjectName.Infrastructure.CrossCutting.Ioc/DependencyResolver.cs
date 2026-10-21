using Cg.ProjectName.Infrastructure.CrossCutting.Ioc.ModuleInitializers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cg.ProjectName.Infrastructure.CrossCutting.Ioc;

public static class DependencyResolver
{
    /// <summary>
    /// Registra as dependências completas para hosts com pipeline HTTP
    /// (hoje só a WebApi): Infrastructure + Application + WebApi (controllers,
    /// health checks, e mensageria no papel de publisher — ver
    /// WebApiModuleInitializer).
    /// </summary>
    public static void RegisterDependecies(IServiceCollection services, IConfiguration configuration)
    {
        RegisterSharedDependencies(services, configuration);

        new WebApiModuleInitializer().Initialize(services, configuration);
    }

    /// <summary>
    /// Registra só o que um host sem pipeline HTTP (Worker/Generic Host)
    /// precisa: Infrastructure + Application (EF Core, Dapper, repositórios,
    /// UnitOfWork, MediatR/pipelines, FluentValidation, jobs). Sem
    /// WebApiModuleInitializer — controllers e health checks HTTP não fazem
    /// sentido aqui — e sem chamar mensageria por conta própria: o Worker
    /// registra o bus com seus próprios consumers direto em Program.cs (ver
    /// comentário em RabbitMqConfiguration), então nada aqui invoca
    /// AddRabbitMqConfiguration de novo — evita registrar o MassTransit duas
    /// vezes no mesmo IServiceCollection.
    ///
    /// Antes desta extração, o Worker nunca chamava nenhum ModuleInitializer
    /// (eles só existiam via RegisterDependecies(WebApplicationBuilder), e o
    /// Worker usa Host.CreateApplicationBuilder) — então IDemoEmployeeRepository,
    /// IUnitOfWork, o DbContext, os pipelines do MediatR etc. simplesmente não
    /// existiam no container do Worker. Na prática, o job recorrente do
    /// Hangfire (demo-employee-maintenance, ver HangfireConfigurationJobs)
    /// falhava ao resolver IDemoEmployeeMaintenanceJob (que depende de
    /// IDemoEmployeeRepository) assim que fosse disparado.
    /// </summary>
    public static void RegisterWorkerDependencies(IServiceCollection services, IConfiguration configuration)
    {
        RegisterSharedDependencies(services, configuration);
    }

    private static void RegisterSharedDependencies(IServiceCollection services, IConfiguration configuration)
    {
        IModuleInitializer[] initializers =
        [
            new InfrastructureModuleInitializer(),
            new ApplicationModuleInitializer()
        ];

        foreach (var initializer in initializers)
        {
            initializer.Initialize(services, configuration);
        }
    }
}
