using Cg.ProjectName.Application;
using Cg.ProjectName.Infrastructure.CrossCutting.Ioc.Configurations;
using Cg.ProjectName.Infrastructure.CrossCutting.Shared.Redis;
using Cg.ProjectName.Infrastructure.CrossCutting.Shared.UnitOfWork;
using Cg.ProjectName.Infrastructure.CrossCutting.Shared.Validation;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Cg.ProjectName.Infrastructure.CrossCutting.Ioc.ModuleInitializers;

public class ApplicationModuleInitializer : IModuleInitializer
{
    public void Initialize(WebApplicationBuilder builder)
    {
        // Reservado para registros da camada de Application
        // (ex: MediatR, AutoMapper/Mapster, validators do FluentValidation, etc.)
        var services = builder.Services;

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
    }
}
