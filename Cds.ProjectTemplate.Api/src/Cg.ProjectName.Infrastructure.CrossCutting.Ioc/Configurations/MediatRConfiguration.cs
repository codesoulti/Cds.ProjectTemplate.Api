using Cg.ProjectName.Application;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Cg.ProjectName.Infrastructure.CrossCutting.Ioc.Configurations;

public static class MediatRConfiguration
{
    /// <summary>
    /// Registra o MediatR e faz o scan dos assemblies de Application e WebApi
    /// (via <see cref="Assembly.GetEntryAssembly"/>, o que evita uma referência
    /// circular do projeto Ioc para o WebApi).
    /// </summary>
    /// <remarks>
    /// Os pipeline behaviors (Validation, Cache, CacheInvalidation, UnitOfWork)
    /// são registrados centralizadamente em <c>ApplicationModuleInitializer</c>,
    /// na ordem em que devem executar — não registre nenhum behavior aqui para
    /// evitar duplicidade de execução.
    /// </remarks>
    public static IServiceCollection AddMediatRConfiguration(this IServiceCollection services)
    {
        var entryAssembly = Assembly.GetEntryAssembly()
            ?? throw new InvalidOperationException(
                "Não foi possível determinar o assembly de entrada da aplicação (Assembly.GetEntryAssembly() retornou null).");

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblies(
               typeof(IApplicationLayer).Assembly,
               entryAssembly
            );
        });

        return services;
    }
}
