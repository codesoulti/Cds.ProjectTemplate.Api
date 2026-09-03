using Cg.ProjectName.Infrastructure.CrossCutting.Ioc.ModuleInitializers;
using Microsoft.AspNetCore.Builder;

namespace Cg.ProjectName.Infrastructure.CrossCutting.Ioc;

public static class DependencyResolver
{
    public static void RegisterDependecies(WebApplicationBuilder builder)
    {
        IModuleInitializer[] initializers =
        [
            new InfrastructureModuleInitializer(),
            new ApplicationModuleInitializer(),
            new WebApiModuleInitializer()
        ];

        foreach (var initializer in initializers)
        {
            initializer.Initialize(builder);
        }
    }
}
