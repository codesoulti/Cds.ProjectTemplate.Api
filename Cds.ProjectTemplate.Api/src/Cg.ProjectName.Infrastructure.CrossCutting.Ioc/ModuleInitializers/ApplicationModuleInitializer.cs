using Cg.ProjectName.Application;
using Cg.ProjectName.Application.Demos.DemoEmployees.Jobs;
using Cg.ProjectName.Infrastructure.CrossCutting.Ioc.Configurations;
using Cg.ProjectName.Infrastructure.CrossCutting.Shared.Redis;
using Cg.ProjectName.Infrastructure.CrossCutting.Shared.UnitOfWork;
using Cg.ProjectName.Infrastructure.CrossCutting.Shared.Validation;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cg.ProjectName.Infrastructure.CrossCutting.Ioc.ModuleInitializers;

public class ApplicationModuleInitializer : IModuleInitializer
{
    public void Initialize(IServiceCollection services, IConfiguration configuration)
    {
        // Reservado para registros da camada de Application
        // (ex: MediatR, AutoMapper/Mapster, validators do FluentValidation, etc.)

        // FluentValidation
        services.AddValidatorsFromAssemblyContaining<IApplicationLayer>();

        services
            .AddMediatRConfiguration()
            .AddAutoMapperConfiguration();

        #region PIPELINES

        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(ValidationBehavior<,>));

        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(CacheBehavior<,>));

        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(CacheInvalidationBehavior<,>));

        services.AddTransient(
            typeof(IPipelineBehavior<,>),
            typeof(UnitOfWorkBehavior<,>));

        #endregion

        #region JOBS
        
        services.AddScoped<
            IDemoEmployeeMaintenanceJob, 
            DemoEmployeeMaintenanceJob>();
        
        #endregion
    }
}
