namespace Cg.ProjectName.Domain.Interfaces.Redis;

public interface ICacheInvalidation
{
    IEnumerable<string> CacheInvalidationKeys { get; }
}
