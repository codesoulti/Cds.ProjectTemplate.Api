using Cg.ProjectName.Infrastructure.CrossCutting.Ioc;
using Cg.ProjectName.Infrastructure.CrossCutting.Ioc.Configurations;
using Cg.ProjectName.Infrastructure.CrossCutting.Shared.Logging;
using Cg.ProjectName.Infrastructure.Data.Contexts.EfCore;
using Cg.ProjectName.Infrastructure.Data.Repositories.Base.Dapper;
using Cg.ProjectName.WebApi.Configurations;
using Cg.ProjectName.WebApi.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Dá tempo do Hangfire Server drenar jobs em execução e de requisições HTTP
// em andamento terminarem antes do processo ser encerrado — o padrão do
// host (5s) é curto demais para um job em background que já começou a
// rodar, e um shutdown no meio de um job pode deixar dado em estado
// inconsistente.
builder.Host.ConfigureHostOptions(options =>
{
    options.ShutdownTimeout = TimeSpan.FromSeconds(30);
});

// Logging estruturado (Serilog) — precisa ser configurado o quanto antes,
// para capturar inclusive falhas durante o startup da aplicação.
builder.AddDefaultLogging();

// Registra as dependências das camadas (Infrastructure, Application, WebApi)
DependencyResolver.RegisterDependecies(builder);

builder.Services.AddCorsConfiguration(builder.Configuration, builder.Environment)
                .AddHangfireConfiguration(builder.Configuration)
                .AddSwaggerConfiguration()
                .AddSignalRConfiguration()
                .AddRateLimitingConfiguration();

// AddControllers() já é chamado por WebApiModuleInitializer (via
// DependencyResolver.RegisterDependecies, acima) — a chamada duplicada que
// existia aqui foi removida para manter uma única fonte de verdade para a
// configuração do MVC.

var app = builder.Build();

// Falha rápido no startup se algum DapperMapping<TEntity, TKey> estiver mal
// configurado (ex.: entidade sem convenção de chave reconhecível), em vez de
// só descobrir isso quando a primeira requisição tocar o repositório.
DapperMappingStartupValidator.ValidateAll();

// Aplica migrations pendentes do EF Core no startup — antes não existia
// NENHUMA estratégia de migração automática nem detecção de schema
// desatualizado: o banco só era atualizado se alguém lembrasse de rodar
// `dotnet ef database update` manualmente antes do deploy. Database:AutoMigrate
// permite desabilitar isso (default true) para deployments controlados que
// preferem rodar a migração como um passo isolado do pipeline de CI/CD.
// Falha aqui é INTENCIONALMENTE fatal (fail-fast): subir a aplicação com um
// schema desatualizado é pior do que não subir.
if (app.Configuration.GetValue("Database:AutoMigrate", defaultValue: true))
{
    using var migrationScope = app.Services.CreateScope();
    var dbContext = migrationScope.ServiceProvider.GetRequiredService<CgProjectNameDbContext>();

    try
    {
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Falha ao aplicar migrations do banco de dados no startup.");
        throw;
    }
}

app.UseDefaultLogging();

// Emite um log estruturado por requisição (método, path, status, duração),
// com as propriedades RequestPath/StatusCode que o filtro em
// LoggingExtension usa para suprimir apenas o ruído de health checks bem
// sucedidos — sem este middleware, aquele filtro não tinha efeito algum
// porque essas propriedades nunca eram emitidas. TraceId é anexado para
// permitir cruzar este log de "requisição finalizada" com qualquer log de
// exceção da mesma requisição (ex.: ValidationExceptionMiddleware).
app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("TraceId", httpContext.TraceIdentifier);
    };
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("DevCors");

app.UseRateLimiter();

// Deve vir antes de qualquer middleware que possa lançar exceções de
// negócio/infraestrutura, para garantir que todas sejam capturadas e
// traduzidas em respostas HTTP consistentes.
app.UseMiddleware<ValidationExceptionMiddleware>();

app.UseSwaggerConfiguration(app.Environment);

// O valor padrão do HSTS é 30 dias — reavalie para cenários de produção,
// veja https://aka.ms/aspnetcore-hsts. Só se aplica fora de Development
// porque HSTS força HTTPS no navegador por um período, o que atrapalha o
// fluxo local sem certificado confiável.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// UseHangfireJobs precisa vir DEPOIS de UseHsts/UseHttpsRedirection — antes
// vinha antes, então o dashboard do Hangfire (/hangfire) ficava acessível em
// HTTP puro, sem o redirecionamento para HTTPS ainda ter sido aplicado ao
// pipeline. A ordem de registro do middleware é a ordem de execução em
// ASP.NET Core; isso importa especialmente aqui porque o dashboard tem sua
// própria superfície de autenticação (ver HangfireConfiguration).
app.UseHangfireJobs(app.Configuration);

app.UseAuthorization();

app.MapControllers();

// Endpoints de liveness/readiness separados — antes havia um único
// "/health" que misturava as duas semânticas: um orquestrador (Kubernetes,
// etc.) usando esse mesmo endpoint para AMBAS as probes reinicia o pod
// (liveness) sempre que uma dependência externa (SQL Server, Redis) fica
// indisponível, mesmo que o processo em si esteja perfeitamente saudável —
// um "restart storm" que não resolve o problema real (a dependência externa
// continua fora do ar) e ainda derruba a única réplica que poderia atender
// outras rotas que não dependem dela.
//
// "/health/live" não executa nenhum check (Predicate = _ => false): só
// responde que o processo está de pé, para a probe de liveness.
// "/health/ready" executa os checks marcados com a tag "ready" (SQL Server,
// Redis quando habilitado — ver WebApiModuleInitializer), para a probe de
// readiness/load balancer decidir se deve rotear tráfego para esta réplica.
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// Mapeia os hubs registrados em SignalRConfiguration — hoje não há nenhum
// hub concreto habilitado (ver comentários em MapSignalRHubs), então isso
// não expõe endpoint algum ainda, mas deixa o scaffold pronto para uso
// assim que um hub real for adicionado, em vez de ficar registrado
// (AddSignalR) e nunca mapeado.
app.MapSignalRHubs();

app.Run();
