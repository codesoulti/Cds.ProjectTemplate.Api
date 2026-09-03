using Cg.ProjectName.Domain.Enums.Shared;

namespace Cg.ProjectName.Application.Demos.DemoEmployees.Queries.ListDemoEmployee;

public class ListDemoEmployeeDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string Document { get; init; } = string.Empty;

    public DateTime DateHire { get; init; }

    public DateTime? DateTermination { get; init; }
    
    public decimal Salary { get; init; }

    public EStatus Status { get; init; }

    public Guid OfficeId { get; init; }

    public string? OfficeName { get; init; }

    // Token de concorrência otimista — reenvie este valor em
    // UpdateDemoEmployeeRequest.RowVersion ao editar este registro, para que
    // o servidor detecte se o dado mudou entre a leitura e a escrita.
    public byte[]? RowVersion { get; init; }
}
