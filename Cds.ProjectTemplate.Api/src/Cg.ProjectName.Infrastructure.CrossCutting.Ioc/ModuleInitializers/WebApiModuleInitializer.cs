using Cg.ProjectName.Infrastructure.CrossCutting.Ioc.Configurations;
using Cg.ProjectName.Infrastructure.CrossCutting.Ioc.HealthChecks;
using Cg.ProjectName.Infrastructure.CrossCutting.Messaging.Configurations;
using Cg.ProjectName.Infrastructure.Data.Contexts.EfCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cg.ProjectName.Infrastructure.CrossCutting.Ioc.ModuleInitializers;

public class WebApiModuleInitializer : IModuleInitializer
{
    public void Initialize(IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers();

        // Checks reais (antes era AddHealthChecks() sem nenhum check
        // registrado — /health respondia "Healthy" mesmo com o SQL Server
        // completamente fora do ar). Redis só entra quando de fato
        // habilitado (ver RedisConfiguration.IsEnabled/AddRedisConfiguration) —
        // do contrário IConnectionMultiplexer nem estaria registrado no DI.
        // Tag "ready": consumida por app.MapHealthChecks("/health/ready", ...)
        // em Program.cs para separar liveness (processo de pé) de readiness
        // (dependências externas realmente acessíveis) — ver comentário lá.
        var healthChecks = services
            .AddHealthChecks()
            .AddCheck<SqlServerHealthCheck>("sql-server", tags: ["ready"]);

        if (RedisConfiguration.IsEnabled(configuration))
        {
            healthChecks.AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);
        }

        // Mensageria — só a WebApi chama daqui (via
        // DependencyResolver.RegisterDependecies), com o papel de PUBLISHER
        // apenas (nenhum "configureConsumers"). Antes esta chamada vivia em
        // InfrastructureModuleInitializer; movida pra cá porque esse método
        // passou a ser compartilhado com o Worker (RegisterWorkerDependencies),
        // que precisa configurar o mesmo bus com seus próprios consumers —
        // mantendo a chamada em InfrastructureModuleInitializer registraria o
        // MassTransit duas vezes quando o Worker também chamasse a sua própria
        // AddRabbitMqConfiguration em Program.cs.
        //
        // useOutbox: true só aqui — só a WebApi publica (ver
        // IIntegrationEventPublisher), então só ela precisa do Outbox
        // Transacional (AddEntityFrameworkOutbox/UseBusOutbox). O Worker
        // (consumer) chama a mesma extensão sem esse parâmetro.
        services.AddRabbitMqConfiguration<CgProjectNameDbContext>(configuration, useOutbox: true);
    }
}
