namespace Cg.ProjectName.WebApi.Controllers.Demos.DeleteDemoEmployee;

/// <summary>
/// Represents a request to delete a demo employee.
/// </summary>
public class DeleteDemoEmployeeRequest
{
    /// <summary>
    /// The unique identifier of the DemoEmployee to retrieve
    /// </summary>
    public Guid Id { get; set; }
}