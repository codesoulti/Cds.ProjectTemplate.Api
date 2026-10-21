using Cg.ProjectName.Domain.Entities.Shared;
using Cg.ProjectName.Domain.Enums.Shared;
using Cg.ProjectName.Domain.Interfaces.Shared;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cg.ProjectName.Domain.Entities.Demos;

// ISoftDelete agora vem de EntityAudititedAndSoftDeletable<Guid> (ver
// Entity.cs) — antes era declarado aqui manualmente, e DemoOffice (mesma
// hierarquia) não o declarava, uma inconsistência que ISoftDelete sendo
// aplicado na base resolve para qualquer entidade soft-deletable, atual
// ou futura, sem depender de lembrete manual.
[Table("Employees", Schema = "Demo")]
public class DemoEmployee 
    : EntityAudititedAndSoftDeletable<Guid>, 
    IHasRowVersion
{
    public required string Name { get; set; }
    
    public required string Document { get; set; }

    public required DateTime DateHire { get; set; }
    
    public DateTime? DateTermination { get; set; }
    
    public required decimal Salary { get; set; }

    public required EStatus Status { get; set; }

    public required Guid OfficeId { get; set; }

    public DemoOffice? Office { get; set; }

    public byte[] RowVersion { get; set; } = [];

    public DemoEmployee()
    {
        CreatedAt = DateTime.UtcNow;
    }

    public static DemoEmployee Create(string name, string document, DateTime dateHire, decimal salary, Guid officeId)
    {
        return new DemoEmployee
        {
            Id = Guid.NewGuid(),
            Name = name,
            Document = document,
            DateHire = dateHire,
            Salary = salary,
            OfficeId = officeId,
            Status = EStatus.Active
        };
    }

    /// <summary>
    /// Aplica as alterações de um comando de atualização sobre a entidade já
    /// rastreada pelo EF Core, preservando encapsulamento (em vez de deixar o
    /// AutoMapper sobrescrever campos diretamente a partir da camada de Application).
    /// Transições de <see cref="Status"/> não passam por aqui de propósito —
    /// use <see cref="Activate"/>/<see cref="Inactivate"/> para isso.
    /// </summary>
    public void Change(string name, string document, DateTime dateHire, DateTime? dateTermination, decimal salary, Guid officeId)
    {
        Name = name;
        Document = document;
        DateHire = dateHire;
        DateTermination = dateTermination;
        Salary = salary;
        OfficeId = officeId;
        UpdatedAt = DateTime.UtcNow;
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
