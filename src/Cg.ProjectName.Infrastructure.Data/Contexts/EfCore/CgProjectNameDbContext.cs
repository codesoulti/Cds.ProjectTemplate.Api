using Cg.ProjectName.Domain.Entities.Demos;
using Cg.ProjectName.Domain.Options.Entities;
using Cg.ProjectName.Infrastructure.Data.Configurations.DemoEmployees;
using Cg.ProjectName.Infrastructure.Data.Configurations.DemoOfficies;
using Cg.ProjectName.Infrastructure.Data.Contexts.Dapper;
using Cg.ProjectName.Infrastructure.Data.Extensions;
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

        base.OnModelCreating(modelBuilder);
    }
}
