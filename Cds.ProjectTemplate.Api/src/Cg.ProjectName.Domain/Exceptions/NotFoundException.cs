namespace Cg.ProjectName.Domain.Exceptions;

public sealed class NotFoundException : Exception
{
    public NotFoundException(string message)
        : base(message)
    {
    }

    public static NotFoundException For<T>(object key) =>
        new($"{typeof(T).Name} with key '{key}' was not found");
}
