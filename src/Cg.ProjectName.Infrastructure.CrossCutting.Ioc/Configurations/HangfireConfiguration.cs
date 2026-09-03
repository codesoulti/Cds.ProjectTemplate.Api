using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cg.ProjectName.Infrastructure.CrossCutting.Ioc.Configurations;

public static class HangfireConfiguration
{
    private const string DefaultDashboardPath = "/hangfire";

    /// <summary>
    /// Registra o Hangfire (servidor + storage) quando habilitado via
    /// configuração (<c>Hangfire:Enabled</c> em appsettings). Ser
    /// configuration-driven, em vez de uma constante fixa no código, permite
    /// ligar/desligar por ambiente sem precisar de um novo deploy.
    /// </summary>
    public static IServiceCollection AddHangfireConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var enabled = configuration.GetValue("Hangfire:Enabled", defaultValue: true);

        if (enabled)
        {
            var connection = configuration.GetConnectionString("DefaultConnection");

            services.AddHangfire(config =>
                config.UseSqlServerStorage(connection));

            services.AddHangfireServer();
        }

        return services;
    }

    /// <summary>
    /// Mapeia o dashboard do Hangfire (quando habilitado) e registra os jobs
    /// recorrentes da aplicação. Nenhum job de exemplo é registrado por
    /// padrão — adicione os seus através de <c>IRecurringJobManager</c> aqui.
    /// </summary>
    public static void UseHangfireJobs(this IApplicationBuilder app, IConfiguration configuration)
    {
        var enabled = configuration.GetValue("Hangfire:Enabled", defaultValue: true);

        if (!enabled)
            return;

        var dashboardPath = configuration["Hangfire:DashboardPath"] ?? DefaultDashboardPath;

        // Ainda não há um esquema de autenticação/autorização configurado
        // nesta aplicação (ver BaseController). Até que exista, restringe o
        // dashboard a requisições locais em vez de deixá-lo público — troque
        // por uma authorization filter real assim que houver autenticação.
        app.UseHangfireDashboard(dashboardPath, new DashboardOptions
        {
            Authorization = [new LocalRequestsOnlyAuthorizationFilter()]
        });

        // Exemplo de registro de job recorrente:
        //
        // using var scope = app.ApplicationServices.CreateScope();
        // var recurringJobs = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        // recurringJobs.AddOrUpdate<MeuJob>("meu-job-id", job => job.Execute(), Cron.Daily);
    }
}
