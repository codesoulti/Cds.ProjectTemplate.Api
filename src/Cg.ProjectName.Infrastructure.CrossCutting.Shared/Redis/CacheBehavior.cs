using Cg.ProjectName.Domain.Interfaces.Redis;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Serilog;
using System.Text.Json;

namespace Cg.ProjectName.Infrastructure.CrossCutting.Shared.Redis;

public class CacheBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IDistributedCache _cache;


    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public CacheBehavior(
        IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {

        if (request is not ICacheableRequest cacheRequest)
        {
            return await next();
        }

        // Assim como no caminho de escrita (abaixo), uma falha do Redis na
        // leitura não pode derrubar a requisição inteira — o comportamento
        // correto é degradar para "cache miss" e seguir para o handler real,
        // não propagar a exceção para fora do pipeline do MediatR.
        string? cachedResponse = null;

        try
        {
            cachedResponse = await _cache.GetStringAsync(cacheRequest.CacheKey, cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "GetStringAsync failed for CacheBehavior, key {CacheKey}", cacheRequest.CacheKey);
        }

        if (!string.IsNullOrEmpty(cachedResponse))
        {
            var deserialized = JsonSerializer.Deserialize<TResponse>(cachedResponse, _options);

            if (deserialized is not null)
                return deserialized;
        }

        var response = await next();

        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(cacheRequest.ExpirationInMinutes)
        };

        try
        {
            var serialized = JsonSerializer.Serialize(response, _options);

            await _cache.SetStringAsync(
                cacheRequest.CacheKey,
                serialized,
                options,
                cancellationToken);
        }
        catch (Exception ex)
        {
            // O comentário antigo dizia "logar erro" mas nada era logado de
            // fato — inconsistente com o catch de leitura logo acima (e com
            // CacheInvalidationBehavior, que já loga). Uma falha persistente
            // de escrita no Redis ficava completamente invisível nos logs.
            Log.Error(ex, "SetStringAsync failed for CacheBehavior, key {CacheKey}", cacheRequest.CacheKey);
        }

        return response;
    }
}
