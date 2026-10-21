namespace Cg.ProjectName.WebApi.Controllers.Demos.CreateDemoEmployee;

/// <summary>
/// Represents a request to create a new demo employee in the system.
/// </summary>
public class CreateDemoEmployeeRequest
{
    public required string Name { get; set; }

    public required string Document { get; set; }

    public required DateTime DateHire { get; set; }

    public DateTime? DateTermination { get; set; }

    public required decimal Salary { get; set; }

    // Status não é aceito na criação: todo funcionário novo nasce Active
    // (ver DemoEmployee.Create). Antes esse campo existia aqui como
    // `required`, obrigando o cliente a enviá-lo, mas o handler nunca o lia
    // — qualquer valor diferente de Active era silenciosamente ignorado.

    public required string OfficeName { get; set; }
}