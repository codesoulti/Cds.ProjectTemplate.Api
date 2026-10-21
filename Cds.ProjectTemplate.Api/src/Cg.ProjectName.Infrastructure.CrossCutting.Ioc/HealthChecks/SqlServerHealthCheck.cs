using Cg.ProjectName.Infrastructure.Data.Contexts.Dapper;
using Dapper;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Cg.ProjectName.Infrastructure.CrossCutting.Ioc.HealthChecks;

/// <summary>
/// Verifica conectividade real com o SQL Server (abre uma conexão e executa
/// "SELECT 1"). Antes, <c>AddHealthChecks()</c> era registrado sem nenhum
/// check — /health sempre respondia 200 "Healthy", mesmo com o banco
/// inteiramente fora do ar, o que torna o endpoint inútil para um probe de
/// liveness/readiness de verdade.
/// </summary>
public sealed class SqlServerHealthCheck(IDbConnectionFactory connectionFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var connection = connectionFactory.CreateConnection();

            await connection.ExecuteScalarAsync(
                new CommandDefinition(
                    "SELECT 1",
                    commandTimeout: 5,
                    cancellationToken: cancellationToken));

            return HealthCheckResult.Healthy("SQL Server disponível.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Não foi possível conectar ao SQL Server.", ex);
        }
    }
}
