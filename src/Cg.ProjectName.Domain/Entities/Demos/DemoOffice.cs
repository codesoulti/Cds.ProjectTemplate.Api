using Cg.ProjectName.Domain.Entities.Shared;
using Cg.ProjectName.Domain.Enums.Shared;
using Cg.ProjectName.Domain.Interfaces.Shared;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cg.ProjectName.Domain.Entities.Demos;

// Nome alinhado com a tabela física real (ver migração "Initial" e
// DemoOfficeConfiguration.ToTable) — antes o atributo dizia "Offices",
// divergindo do nome de tabela usado por EF Core e Dapper ("DemoOffices").
[Table("Offices", Schema = "Demo")]
public class DemoOffice : EntityAudititedAndSoftDeletable<Guid>, IHasRowVersion
{
    public required string Name { get; set; }

    public required EStatus Status { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public ICollection<DemoEmployee> Employees { get; set; } = [];

    public DemoOffice()
    {
        CreatedAt = DateTime.UtcNow;
    }

    public static DemoOffice Create(string name)
    {
        return new DemoOffice
        {
            Id = Guid.NewGuid(),
            Name = name,
            Status = EStatus.Active
        };
    }

    public void Inactivate()
    {
        Status = EStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        Status = EStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }
}
