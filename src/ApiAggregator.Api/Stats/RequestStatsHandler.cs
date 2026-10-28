namespace ApiAggregator.Api.Stats;

public sealed class RequestStatsHandler(RequestStatsStore store, TimeProvider timeProvider, string api) : DelegatingHandler
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
            store.Record(api, timeProvider.GetElapsedTime(start));
        }
    }
}