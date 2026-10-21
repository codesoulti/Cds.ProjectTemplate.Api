using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cg.ProjectName.Infrastructure.CrossCutting.Ioc.ModuleInitializers;

// Antes recebia um WebApplicationBuilder inteiro — só para poder chamar
// .Services/.Configuration, que também existem em qualquer outro builder de
// host (ex.: HostApplicationBuilder, usado pelo Worker). Trocado por
// IServiceCollection + IConfiguration direto, para que os mesmos
// ModuleInitializers sirvam tanto a WebApi (WebApplicationBuilder) quanto o
// Worker (Generic Host puro) — ver DependencyResolver.RegisterWorkerDependencies.
public interface IModuleInitializer
{
    void Initialize(IServiceCollection services, IConfiguration configuration);
}
