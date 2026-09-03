using System.Threading.RateLimiting;

namespace Cg.ProjectName.WebApi.Configurations;

/// <summary>
/// Rate limiting global por IP de origem — antes não havia NENHUM limite,
/// então qualquer endpoint podia ser usado para brute-force ou negação de
/// serviço (DoS) sem nenhuma barreira. Usa o rate limiter nativo do
/// ASP.NET Core (Microsoft.AspNetCore.RateLimiting, disponível desde o
/// .NET 7 sem pacote adicional), particionado por IP remoto — cada endereço
/// tem sua própria janela de limite, então um cliente abusivo não consome a
/// cota de outros clientes legítimos.
/// </summary>
public static class RateLimitingConfiguration
{
    // Valores de partida razoáveis para um template — ajuste conforme o
    // perfil de tráfego real da aplicação (ex.: limites por rota/endpoint,
    // ou uma política diferenciada para endpoints de escrita).
    private const int PermitLimit = 100;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddRateLimitingConfiguration(
        this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = PermitLimit,
                        Window = Window,
                        QueueLimit = 0,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }));
        });

        return services;
    }
}
