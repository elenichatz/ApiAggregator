namespace ApiAggregator.Api.Stats;

public sealed class RequestStatsHandler(RequestStatsStore store, TimeProvider timeProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var start = timeProvider.GetTimestamp();
        try
        {
            return await base.SendAsync(request, cancellationToken);
        }
        finally
        {
            store.Record(request.RequestUri?.Host ?? "unknown", timeProvider.GetElapsedTime(start));
        }
    }
}