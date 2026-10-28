using ApiAggregator.Api.Clients;
using ApiAggregator.Api.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace ApiAggregator.Api.Services;

public sealed class AggregationService(IEnumerable<IExternalApiClient> clients, IMemoryCache cache,
    IOptions<CacheOptions> cacheOptions, TimeProvider timeProvider,ILogger<AggregationService> logger): IAggregationService
{
    public async Task<AggregationResult> AggregateAsync(FetchRequest request,
        CancellationToken cancellationToken)
    {
        var tasks = clients.Select(client => FetchSafelyAsync(client, request, cancellationToken));
        var results = await Task.WhenAll(tasks);
        return new AggregationResult(Items: results.SelectMany(result => result.Items).ToList(),
            Sources: results.Select(result => result.Status).ToList());
    }

    private async Task<SourceResult> FetchSafelyAsync(IExternalApiClient client, FetchRequest request,
        CancellationToken cancellationToken)
    {
        if (!client.CanHandle(request))
        {
            return new SourceResult([], new SourceStatus(client.Name, SourceState.Skipped, Error: null));
        }
        var options = cacheOptions.Value;
        var cacheKey = CacheKey(client, request);
        var now = timeProvider.GetUtcNow();

        var cached = cache.Get<CachedItems>(cacheKey);
        if (cached is not null && now - cached.FetchedAt < options.FreshFor)
        {
            return new SourceResult(cached.Items, new SourceStatus(client.Name, SourceState.Cached, Error: null));
        }
        
        try
        {
            var items = await client.FetchAsync(request, cancellationToken);
            cache.Set(cacheKey, new CachedItems(items, now), new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = options.StaleFor,
                Size = 1
            });
            return new SourceResult(items, new SourceStatus(client.Name, SourceState.Succeeded, Error: null));
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Source {Source} failed", client.Name);
            if (cached is not null)
            {
                return new SourceResult(cached.Items, new SourceStatus(client.Name, SourceState.Stale, Describe(exception)));
            }
            return new SourceResult([], new SourceStatus(client.Name, SourceState.Failed, Describe(exception)));
        }
    }
    
    private static string CacheKey(IExternalApiClient client, FetchRequest request) =>
        $"{client.Name}|{Normalize(request.Keyword)}|{Normalize(request.City)}";

    private static string Normalize(string? value) => value?.Trim().ToLowerInvariant() ?? "";
    
    private static string Describe(Exception exception) => exception switch
    {
        HttpRequestException { StatusCode: not null } httpException => $"HTTP {(int)httpException.StatusCode}",
        TimeoutRejectedException => "Timed out",
        BrokenCircuitException => "Circuit open",
        _ => "Unexpected error"
    };

    private sealed record SourceResult(IReadOnlyList<AggregatedItem> Items, SourceStatus Status);
    
    private sealed record CachedItems(IReadOnlyList<AggregatedItem> Items, DateTimeOffset FetchedAt);
    
}