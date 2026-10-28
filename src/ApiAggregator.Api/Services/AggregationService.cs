using ApiAggregator.Api.Clients;
using ApiAggregator.Api.Models;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace ApiAggregator.Api.Services;

public sealed class AggregationService(IEnumerable<IExternalApiClient> clients, ILogger<AggregationService> logger): IAggregationService
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
        try
        {
            var items = await client.FetchAsync(request, cancellationToken);

            return new SourceResult(items, new SourceStatus(client.Name, SourceState.Succeeded, Error: null));
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Source {Source} failed", client.Name);

            return new SourceResult([], new SourceStatus(client.Name, SourceState.Failed, Describe(exception)));
        }
    }
    
    private static string Describe(Exception exception) => exception switch
    {
        HttpRequestException { StatusCode: not null } httpException => $"HTTP {(int)httpException.StatusCode}",
        TimeoutRejectedException => "Timed out",
        BrokenCircuitException => "Circuit open",
        _ => "Unexpected error"
    };

    private sealed record SourceResult(IReadOnlyList<AggregatedItem> Items, SourceStatus Status);

}