using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace Cg.ProjectName.Infrastructure.CrossCutting.Ioc.Configurations;

public static class RedisConfiguration
{
    /// <summary>
    /// Registra o cache distribuído. Segue o mesmo padrão de
    /// <c>HangfireConfiguration</c>: controlado por <c>Redis:Enabled</c> em
    /// appsettings, para poder ligar/desligar por ambiente sem novo deploy.
    /// Diferente da versão anterior, Redis deixou de ser uma dependência
    /// obrigatória de startup — quando desabilitado, ou quando a connection
    /// string não está configurada, a aplicação cai para um
    /// <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/>
    /// em memória, para que <c>CacheBehavior</c>/<c>CacheInvalidationBehavior</c>
    /// (registrados globalmente no pipeline do MediatR) continuem resolvendo
    /// normalmente em vez de falhar a primeira requisição por falta de
    /// implementação registrada.
    /// </summary>
    /// <summary>
    /// Único ponto de verdade para "Redis está realmente ligado?" — reusado
    /// pelo health check (ver HealthChecks/RedisHealthCheck) para decidir se
    /// deve se registrar, evitando duplicar (e arriscar divergir) a mesma
    /// checagem de configuração em dois lugares.
    /// </summary>
    public static bool IsEnabled(IConfiguration configuration)
    {
        var enabled = configuration.GetValue("Redis:Enabled", defaultValue: true);
        var redisConnection = configuration.GetSection("Redis:ConnectionString")?.Value;

        return enabled && !string.IsNullOrWhiteSpace(redisConnection);
    }

    public static IServiceCollection AddRedisConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (!IsEnabled(configuration))
        {
            services.AddDistributedMemoryCache();

            return services;
        }

        var redisConnection = configuration.GetSection("Redis:ConnectionString")!.Value;

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
            options.InstanceName = configuration.GetSection("Redis:InstanceName")?.Value;
        });

        services.AddSingleton<IConnectionMultiplexer>(
            _ => ConnectionMultiplexer.Connect(redisConnection!));

        return services;
    }
}
