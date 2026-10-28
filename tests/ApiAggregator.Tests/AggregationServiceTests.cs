using System.Net;
using ApiAggregator.Api.Models;
using ApiAggregator.Api.Services;
using ApiAggregator.Tests.Fakes;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;

namespace ApiAggregator.Tests;

public class AggregationServiceTests
{
    private static readonly FetchRequest Request = new("dotnet", "Athens");

    private readonly FakeTimeProvider timeProvider = new();
    private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = 100 });

    private AggregationService CreateService(params FakeApiClient[] clients) =>
        new(clients, cache, Options.Create(new CacheOptions()), timeProvider,
            NullLogger<AggregationService>.Instance);

    [Fact]
    public async Task AggregateAsync_AllSourcesSucceed_CombinesItemsFromEverySource()
    {
        var news = new FakeApiClient("news") { Respond = () => [TestItems.Create("a"), TestItems.Create("b")] };
        var code = new FakeApiClient("code") { Respond = () => [TestItems.Create("c")] };

        var result = await CreateService(news, code).AggregateAsync(Request, CancellationToken.None);

        Assert.Equal(3, result.Items.Count);
        Assert.All(result.Sources, source => Assert.Equal(SourceState.Succeeded, source.State));
    }

    [Fact]
    public async Task AggregateAsync_OneSourceFails_ReturnsItemsFromTheOthers()
    {
        var working = new FakeApiClient("working") { Respond = () => [TestItems.Create("a")] };
        var broken = new FakeApiClient("broken")
        {
            Respond = () => throw new HttpRequestException("boom", null, HttpStatusCode.ServiceUnavailable)
        };

        var result = await CreateService(working, broken).AggregateAsync(Request, CancellationToken.None);

        Assert.Single(result.Items);
        var status = Assert.Single(result.Sources, source => source.Name == "broken");
        Assert.Equal(SourceState.Failed, status.State);
        Assert.Equal("HTTP 503", status.Error);
    }

    [Fact]
    public async Task AggregateAsync_SourceCannotHandleRequest_IsSkippedWithoutBeingCalled()
    {
        var client = new FakeApiClient("weather", canHandle: false);

        var result = await CreateService(client).AggregateAsync(Request, CancellationToken.None);

        Assert.Equal(SourceState.Skipped, Assert.Single(result.Sources).State);
        Assert.Equal(0, client.CallCount);
    }

    [Fact]
    public async Task AggregateAsync_SameRequestWithinFreshWindow_ServesFromCache()
    {
        var client = new FakeApiClient("news") { Respond = () => [TestItems.Create("a")] };
        var service = CreateService(client);

        await service.AggregateAsync(new FetchRequest("dotnet", "Athens"), CancellationToken.None);
        timeProvider.Advance(TimeSpan.FromMinutes(4));
        var result = await service.AggregateAsync(new FetchRequest(" DotNet ", "athens"), CancellationToken.None);

        Assert.Equal(1, client.CallCount);
        Assert.Equal(SourceState.Cached, Assert.Single(result.Sources).State);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task AggregateAsync_AfterFreshWindow_CallsSourceAgain()
    {
        var client = new FakeApiClient("news") { Respond = () => [TestItems.Create("a")] };
        var service = CreateService(client);

        await service.AggregateAsync(Request, CancellationToken.None);
        timeProvider.Advance(TimeSpan.FromMinutes(6));
        var result = await service.AggregateAsync(Request, CancellationToken.None);

        Assert.Equal(2, client.CallCount);
        Assert.Equal(SourceState.Succeeded, Assert.Single(result.Sources).State);
    }

    [Fact]
    public async Task AggregateAsync_SourceFailsAfterFreshWindow_ReturnsStaleItems()
    {
        var client = new FakeApiClient("news") { Respond = () => [TestItems.Create("old")] };
        var service = CreateService(client);
        await service.AggregateAsync(Request, CancellationToken.None);

        client.Respond = () => throw new HttpRequestException("boom", null, HttpStatusCode.InternalServerError);
        timeProvider.Advance(TimeSpan.FromMinutes(6));
        var result = await service.AggregateAsync(Request, CancellationToken.None);

        var status = Assert.Single(result.Sources);
        Assert.Equal(SourceState.Stale, status.State);
        Assert.Equal("HTTP 500", status.Error);
        Assert.Equal("old", Assert.Single(result.Items).Title);
    }

    [Fact]
    public async Task AggregateAsync_RequestCancelled_ThrowsInsteadOfReportingFailure()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var client = new FakeApiClient("news") { Respond = () => throw new OperationCanceledException() };

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            CreateService(client).AggregateAsync(Request, cancellation.Token));
    }
}