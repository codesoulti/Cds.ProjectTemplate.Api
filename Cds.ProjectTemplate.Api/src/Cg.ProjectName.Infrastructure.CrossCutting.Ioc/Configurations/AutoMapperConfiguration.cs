using Cg.ProjectName.Application;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Cg.ProjectName.Infrastructure.CrossCutting.Ioc.Configurations;

public static class AutoMapperConfiguration
{
    public static IServiceCollection AddAutoMapperConfiguration(this IServiceCollection services)
    {
        services.AddAutoMapper(
            _ => { },
            Assembly.GetEntryAssembly(),
            typeof(IApplicationLayer).Assembly
        );

        return services;
    }
}
