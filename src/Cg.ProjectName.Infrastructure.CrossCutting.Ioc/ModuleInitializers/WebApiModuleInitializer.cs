using Cg.ProjectName.Infrastructure.CrossCutting.Ioc.Configurations;
using Cg.ProjectName.Infrastructure.CrossCutting.Ioc.HealthChecks;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cg.ProjectName.Infrastructure.CrossCutting.Ioc.ModuleInitializers;

public class WebApiModuleInitializer : IModuleInitializer
{
    public void Initialize(WebApplicationBuilder builder)
    {
        var services = builder.Services;

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

        if (RedisConfiguration.IsEnabled(builder.Configuration))
        {
            healthChecks.AddCheck<RedisHealthCheck>("redis", tags: ["ready"]);
        }
    }
}
