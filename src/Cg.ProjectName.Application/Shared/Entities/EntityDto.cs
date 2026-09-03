namespace Cg.ProjectName.Application.Shared.Entities;

public abstract class EntityDto<T> where T 
    : IEquatable<T>
{
    public required T Id { get; set; }
}
