using System.Net;
using ApiAggregator.Api.Stats;
using ApiAggregator.Tests.Fakes;
using Microsoft.Extensions.Time.Testing;

namespace ApiAggregator.Tests;

public class RequestStatsHandlerTests
{
    private readonly RequestStatsStore store = new();
    private readonly FakeTimeProvider timeProvider = new();

    private HttpClient CreateClient(Func<HttpRequestMessage, HttpResponseMessage> respond) =>
        new(new RequestStatsHandler(store, timeProvider, "example") { InnerHandler = new StubHttpMessageHandler(respond) });

    [Fact]
    public async Task SendAsync_RecordsElapsedTimeUnderTheApiName()
    {
        using var client = CreateClient(_ =>
        {
            timeProvider.Advance(TimeSpan.FromMilliseconds(150));
            return new HttpResponseMessage(HttpStatusCode.OK);
        });

        await client.GetAsync("https://api.example.com/search?q=x");

        var stats = Assert.Single(store.GetSnapshot());
        Assert.Equal("example", stats.Api);
        Assert.Equal(150, stats.AverageResponseMs);
        Assert.Equal(1, stats.Average.Requests);
    }

    [Fact]
    public async Task SendAsync_RequestThrows_StillRecordsIt()
    {
        using var client = CreateClient(_ => throw new HttpRequestException("network down"));

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAsync("https://api.example.com/"));

        Assert.Equal(1, Assert.Single(store.GetSnapshot()).TotalRequests);
    }
}