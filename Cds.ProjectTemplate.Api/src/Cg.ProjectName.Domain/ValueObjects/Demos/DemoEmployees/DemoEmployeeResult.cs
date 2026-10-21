using Cg.ProjectName.Domain.Enums.Shared;

namespace Cg.ProjectName.Domain.ValueObjects.Demos.DemoEmployees;

public sealed class DemoEmployeeResult
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Document { get; init; } = string.Empty;
    public EStatus Status { get; init; }
    public Guid OfficeId { get; init; }
    public string? OfficeName { get; init; }
    public byte[] RowVersion { get; init; } = [];
}
