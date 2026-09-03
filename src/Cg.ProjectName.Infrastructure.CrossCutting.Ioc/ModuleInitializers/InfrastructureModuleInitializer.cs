using Cg.ProjectName.Application.Interfaces.Shared;
using Cg.ProjectName.Domain.Interfaces.Repositories;
using Cg.ProjectName.Domain.Interfaces.Repositories.Demos.DemoEmployees;
using Cg.ProjectName.Domain.Interfaces.Repositories.Demos.DemoOfficies;
using Cg.ProjectName.Infrastructure.CrossCutting.Ioc.Configurations;
using Cg.ProjectName.Infrastructure.Data.Contexts.Dapper;
using Cg.ProjectName.Infrastructure.Data.Contexts.EfCore;
using Cg.ProjectName.Infrastructure.Data.Interceptors;
using Cg.ProjectName.Infrastructure.Data.Repositories.Base.EfCore;
using Cg.ProjectName.Infrastructure.Data.Repositories.Demos.DemoEmployees;
using Cg.ProjectName.Infrastructure.Data.Repositories.Demos.DemoOfficies;
using Cg.ProjectName.Infrastructure.Data.UnitOfWork;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cg.ProjectName.Infrastructure.CrossCutting.Ioc.ModuleInitializers;

public class InfrastructureModuleInitializer : IModuleInitializer
{
    public void Initialize(WebApplicationBuilder builder)
    {
        var services = builder.Services;

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' não configurada em appsettings.json.");

        services
            .AddHangfireConfiguration(builder.Configuration)
            .AddRedisConfiguration(builder.Configuration);

        // ---- EF Core: lado de escrita (comandos / agregados / Unit of Work) ----
        services.AddScoped<SoftDeleteInterceptor>();

        services.AddDbContext<CgProjectNameDbContext>((serviceProvider, options) =>
        {
            var interceptor = serviceProvider
                .GetRequiredService<SoftDeleteInterceptor>();

            options
                .UseSqlServer(connectionString, sqlServerOptions =>
                    // Sem isso, uma falha transitória de rede/failover contra o
                    // SQL Server (comum em Azure SQL, e não incomum em qualquer
                    // SQL Server sob load balancer) vira uma SqlException crua
                    // na primeira tentativa, em vez de uma nova tentativa
                    // automática. Combinado com UnitOfWork.ExecuteInStrategyAsync
                    // (ver UnitOfWorkBehavior/UnitOfWork), que é obrigatório
                    // assim que EnableRetryOnFailure está ligado e existem
                    // transações iniciadas manualmente pela aplicação.
                    sqlServerOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null))
                .AddInterceptors(interceptor);
        });

        services.AddScoped(typeof(IEFRepository<,>), typeof(EfRepository<,>));
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // ---- Dapper: lado de leitura (queries otimizadas, sem tracking) ----
        services.AddSingleton<IDbConnectionFactory>(
            _ => new SqlConnectionFactory(connectionString));

        services.AddScoped<IDemoEmployeeReadRepository, DemoEmployeeReadRepository>();
        services.AddScoped<IDemoEmployeeWriterRepository, DemoEmployeeWriterRepository>();
        services.AddScoped<IDemoOfficeReadRepository, DemoOfficeReadRepository>();
        services.AddScoped<IDemoOfficeWriterRepository, DemoOfficeWriterRepository>();
    }
}
