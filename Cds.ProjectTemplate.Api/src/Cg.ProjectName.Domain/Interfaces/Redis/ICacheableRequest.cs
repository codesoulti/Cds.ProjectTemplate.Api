namespace Cg.ProjectName.Domain.Interfaces.Redis;

public interface ICacheableRequest
{
    string CacheKey { get; }
    int ExpirationInMinutes { get; }
}

