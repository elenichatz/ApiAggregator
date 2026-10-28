namespace ApiAggregator.Api.Stats;

public static class HttpClientBuilderExtensions
{
    /// <summary>Records every request of this client in the stats, under the client's name.</summary>
    public static IHttpClientBuilder AddRequestStats(this IHttpClientBuilder builder) =>
        builder.AddHttpMessageHandler(services => new RequestStatsHandler(
            services.GetRequiredService<RequestStatsStore>(),
            services.GetRequiredService<TimeProvider>(),
            builder.Name));
}