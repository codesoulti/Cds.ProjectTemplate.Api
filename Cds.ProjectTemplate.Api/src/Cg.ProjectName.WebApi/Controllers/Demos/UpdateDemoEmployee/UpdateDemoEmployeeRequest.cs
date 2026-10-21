using Cg.ProjectName.Domain.Enums.Shared;

namespace Cg.ProjectName.WebApi.Controllers.Demos.UpdateDemoEmployee;

/// <summary>
/// Represents a request to update an existing demo employee in the system.
/// </summary>
public class UpdateDemoEmployeeRequest 
{
    /// <summary>
    /// The unique identifier of the DemoEmployee to retrieve
    /// </summary>
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Document { get; set; }

    public required DateTime DateHire { get; set; }

    public DateTime? DateTermination { get; set; }

    public required decimal Salary { get; set; }

    public required EStatus Status { get; set; }

    public required string OfficeName { get; set; }

    /// <summary>
    /// Token de concorrência (RowVersion) obtido de uma leitura anterior
    /// (GET /api/DemoEmployee/{id} ou GET /api/DemoEmployee/List) do mesmo
    /// recurso. Opcional, mas recomendado: sem ele, o servidor não consegue
    /// detectar que este PUT parte de um dado já desatualizado, e a última
    /// escrita simplesmente sobrescreve a anterior sem aviso.
    /// </summary>
    public byte[]? RowVersion { get; set; }
}