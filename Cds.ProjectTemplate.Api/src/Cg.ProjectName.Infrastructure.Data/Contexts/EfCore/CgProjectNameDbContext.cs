using Cg.ProjectName.Domain.Entities.Demos;
using Cg.ProjectName.Domain.Options.Entities;
using Cg.ProjectName.Infrastructure.Data.Configurations.DemoEmployees;
using Cg.ProjectName.Infrastructure.Data.Configurations.DemoOfficies;
using Cg.ProjectName.Infrastructure.Data.Contexts.Dapper;
using Cg.ProjectName.Infrastructure.Data.Extensions;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Cg.ProjectName.Infrastructure.Data.Contexts.EfCore;

/// <summary>
/// DbContext do EF Core - usado no lado de escrita (comandos) da abordagem híbrida.
/// As consultas (lado de leitura) são feitas via Dapper, através de <see cref="IDbConnectionFactory"/>.
/// </summary>
public class CgProjectNameDbContext : DbContext
{
    public CgProjectNameDbContext(DbContextOptions<CgProjectNameDbContext> options)
        : base(options)
    {
    }

    public DbSet<DemoEmployee> DemoEmployees => Set<DemoEmployee>();

    public DbSet<DemoOffice> DemoOffices => Set<DemoOffice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new DemoEmployeeConfiguration());
        modelBuilder.ApplyConfiguration(new DemoOfficeConfiguration());

        modelBuilder.ApplySoftDelete(SoftDeleteOptions.Enabled);

        // Tabelas do Outbox Transacional do MassTransit (pacote
        // MassTransit.EntityFrameworkCore) — usadas só quando
        // AddRabbitMqConfiguration<TDbContext> é chamado com useOutbox: true
        // (hoje, só pela WebApi; ver RabbitMqConfiguration/WebApiModuleInitializer).
        // Precisa de uma migration própria (dotnet ef migrations add) depois
        // desta mudança — não gerada automaticamente. AddInboxStateEntity()
        // fica registrada mesmo sem uso hoje (nenhum consumer usa outbox do
        // lado de recebimento) para não exigir uma segunda migration no dia
        // em que isso for adotado.
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();

        base.OnModelCreating(modelBuilder);
    }
}
