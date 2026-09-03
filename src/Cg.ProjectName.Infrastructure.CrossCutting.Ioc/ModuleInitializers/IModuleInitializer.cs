using Microsoft.AspNetCore.Builder;

namespace Cg.ProjectName.Infrastructure.CrossCutting.Ioc.ModuleInitializers;

public interface IModuleInitializer
{
    void Initialize(WebApplicationBuilder builder);
}
