namespace Cg.ProjectName.WebApi.Controllers.Demos.GetDemoEmployee;

/// <summary>
/// Request model for getting a DemoEmployee by ID
/// </summary>
public class GetDemoEmployeeRequest
{
    /// <summary>
    /// The unique identifier of the DemoEmployee to retrieve
    /// </summary>
    public Guid Id { get; set; }
}
