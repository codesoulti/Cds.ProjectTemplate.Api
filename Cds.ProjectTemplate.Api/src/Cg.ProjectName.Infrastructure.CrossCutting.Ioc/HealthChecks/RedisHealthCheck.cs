using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Cg.ProjectName.Infrastructure.CrossCutting.Ioc.HealthChecks;

/// <summary>
/// Verifica conectividade real com o Redis via PING. Só é registrado quando
/// Redis está de fato habilitado (ver <see cref="Configurations.RedisConfiguration.IsEnabled"/>)
/// — quando desabilitado, <see cref="IConnectionMultiplexer"/> nem é
/// registrado no container de DI, então tentar resolver este health check
/// nesse caso lançaria na primeira execução em vez de simplesmente reportar
/// "unhealthy".
/// </summary>
public sealed class RedisHealthCheck(IConnectionMultiplexer connectionMultiplexer) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var latency = await connectionMultiplexer.GetDatabase().PingAsync();

            return HealthCheckResult.Healthy($"Redis disponível ({latency.TotalMilliseconds:0}ms).");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Não foi possível conectar ao Redis.", ex);
        }
    }
}
