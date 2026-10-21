using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Exceptions;
using Serilog.Exceptions.Core;
using Serilog.Sinks.SystemConsole.Themes;

namespace Cg.ProjectName.Infrastructure.CrossCutting.Shared.Logging;

/// <summary> Add default Logging configuration to project. This configuration supports Serilog logs with DataDog compatible output.</summary>
public static class LoggingExtension
{
    /// <summary>
    /// The destructuring options builder configured with default destructurers and a custom DbUpdateExceptionDestructurer.
    /// </summary>
    static readonly DestructuringOptionsBuilder _destructuringOptionsBuilder =
    new DestructuringOptionsBuilder()
        .WithDefaultDestructurers();

    /// <summary>
    /// A filter predicate to exclude log events with specific criteria.
    /// </summary>
    /// <remarks>
    /// A lógica estava invertida: "if (Level != Information) return true"
    /// fazia com que TODO evento de Warning/Error/Fatal fosse excluído dos
    /// sinks (console e arquivo) — ou seja, exceções não tratadas
    /// (ValidationExceptionMiddleware) e falhas de cache
    /// (CacheInvalidationBehavior) nunca apareciam em lugar nenhum. A regra
    /// deve ser o oposto: nunca excluir nada além de Information, e mesmo
    /// assim só suprimir o ruído específico de um health check bem-sucedido
    /// (200 em /health) — qualquer outro Information, e qualquer nível acima
    /// de Information, sempre passa.
    /// </remarks>
    static readonly Func<LogEvent, bool> _filterPredicate = exclusionPredicate =>
    {

        if (exclusionPredicate.Level != LogEventLevel.Information) return false;

        // Propriedades emitidas por UseSerilogRequestLogging (Serilog.AspNetCore).
        exclusionPredicate.Properties.TryGetValue("StatusCode", out var statusCode);
        exclusionPredicate.Properties.TryGetValue("RequestPath", out var path);

        var isStatusCode200 = statusCode?.ToString() == "200";
        var isHealthPath = path?.ToString().Contains("/health") ?? false;

        return isStatusCode200 && isHealthPath;
    };

    /// <summary>
    /// This method configures the logging with commonly used features for DataDog integration.
    /// </summary>
    /// <param name="builder">The <see cref="WebApplicationBuilder" /> to add services to.</param>
    /// <returns>A <see cref="WebApplicationBuilder"/> that can be used to further configure the API services.</returns>
    /// <remarks>
    /// <para>Logging output are diferents on Debug and Release modes.</para>
    /// </remarks> 
    public static WebApplicationBuilder AddDefaultLogging(this WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration().CreateLogger();
        builder.Host.UseSerilog((hostingContext, loggerConfiguration) =>
        {
            loggerConfiguration
                .ReadFrom.Configuration(hostingContext.Configuration)
                .Enrich.WithMachineName()
                .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
                .Enrich.WithProperty("Application", builder.Environment.ApplicationName)
                .Enrich.FromLogContext()
                .Enrich.WithExceptionDetails(_destructuringOptionsBuilder)
                .Filter.ByExcluding(_filterPredicate);

            // Antes o critério era Debugger.IsAttached: um debugger anexado a
            // um processo de PRODUÇÃO durante um incidente mudava o formato
            // de log em produção (e, mais grave, alguém rodando `dotnet run`
            // localmente sem debugger anexado caía no branch de "produção").
            // O ambiente (IsDevelopment) é o sinal correto — é fixo por
            // deployment, não depende de uma ferramenta estar ou não anexada
            // ao processo naquele instante.
            if (hostingContext.HostingEnvironment.IsDevelopment())
            {
                loggerConfiguration.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}", theme: SystemConsoleTheme.Colored);
            }
            else
            {
                loggerConfiguration
                    .WriteTo.Console
                    (
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"
                    );

                // Sink de arquivo local — pressupõe um filesystem gravável e
                // persistente, um encaixe ruim para a maioria dos deployments
                // em container (root filesystem somente-leitura, storage
                // efêmero). O Console acima já é suficiente para qualquer
                // coletor de log de container (stdout/stderr); o arquivo fica
                // como opção complementar para deploys tradicionais em VM,
                // desligável via "Logging:EnableFileSink": false sem precisar
                // recompilar.
                if (hostingContext.Configuration.GetValue("Logging:EnableFileSink", defaultValue: true))
                {
                    loggerConfiguration.WriteTo.File(
                        "logs/log-.txt",
                        rollingInterval: RollingInterval.Day,
                        // Sem um limite, um deploy de longa duração acumula um
                        // arquivo por dia para sempre, sem nenhuma eviction,
                        // até encher o disco. 30 dias é um valor de partida
                        // razoável para um template — ajuste conforme a
                        // política de retenção de log do ambiente real.
                        retainedFileCountLimit: 30,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}"
                    );
                }
            }
        });

        builder.Services.AddLogging();

        return builder;
    }

    /// <summary>Adds middleware for Swagger documetation generation.</summary>
    /// <param name="app">The <see cref="WebApplication"/> instance this method extends.</param>
    /// <returns>The <see cref="WebApplication"/> for Swagger documentation.</returns>
    public static WebApplication UseDefaultLogging(this WebApplication app)
    {
        var logger = app.Services.GetRequiredService<ILogger<Logger>>();

        var mode = app.Environment.IsDevelopment() ? "Development" : "Production-like";
        logger.LogInformation("Logging enabled for '{Application}' on '{Environment}' - Mode: {Mode}", app.Environment.ApplicationName, app.Environment.EnvironmentName, mode);
        return app;

    }
}
