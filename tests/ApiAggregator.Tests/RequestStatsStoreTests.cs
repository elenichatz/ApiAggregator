using ApiAggregator.Api.Stats;

namespace ApiAggregator.Tests;

public class RequestStatsStoreTests
{
    private readonly RequestStatsStore store = new();

    [Fact]
    public void GetSnapshot_NoRequests_IsEmpty()
    {
        Assert.Empty(store.GetSnapshot());
    }

    [Fact]
    public void Record_PutsEachRequestInTheRightBucket()
    {
        foreach (var milliseconds in new[] { 50, 100, 150, 200, 250 })
        {
            store.Record("api", TimeSpan.FromMilliseconds(milliseconds));
        }

        var stats = Assert.Single(store.GetSnapshot());
        Assert.Equal(5, stats.TotalRequests);
        Assert.Equal(150, stats.AverageResponseMs);
        Assert.Equal(1, stats.Fast);
        Assert.Equal(3, stats.Average);
        Assert.Equal(1, stats.Slow);
    }

    [Fact]
    public void Record_KeepsSeparateStatsPerApi()
    {
        store.Record("b-api", TimeSpan.FromMilliseconds(10));
        store.Record("a-api", TimeSpan.FromMilliseconds(300));
        store.Record("a-api", TimeSpan.FromMilliseconds(500));

        var snapshot = store.GetSnapshot();

        Assert.Equal(["a-api", "b-api"], snapshot.Select(stats => stats.Api));
        Assert.Equal(2, snapshot[0].TotalRequests);
        Assert.Equal(400, snapshot[0].AverageResponseMs);
    }

    [Fact]
    public void Record_FromManyThreadsAtOnce_LosesNoRequests()
    {
        Parallel.For(0, 10_000, i => store.Record($"api-{i % 3}", TimeSpan.FromMilliseconds(i % 300)));

        Assert.Equal(10_000, store.GetSnapshot().Sum(stats => stats.TotalRequests));
    }
}