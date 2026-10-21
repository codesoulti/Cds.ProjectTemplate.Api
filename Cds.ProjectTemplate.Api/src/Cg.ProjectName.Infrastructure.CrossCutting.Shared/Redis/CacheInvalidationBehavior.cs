using Cg.ProjectName.Domain.Interfaces.Redis;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;

namespace Cg.ProjectName.Infrastructure.CrossCutting.Shared.Redis;

public class CacheInvalidationBehavior<TRequest, TResponse>
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
{
    private readonly IDistributedCache _redis;

    public CacheInvalidationBehavior(IDistributedCache redis)
    {
        _redis = redis;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {

        var response = await next();

        if (request is ICacheInvalidation invalidation)
        {
            foreach (var key in invalidation.CacheInvalidationKeys)
            {
                try
                {
                    await _redis.RemoveAsync(key, cancellationToken);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "RemoveAsync failed for CacheInvalidationBehavior");
                }
            }
        }

        return response;
    }
}
