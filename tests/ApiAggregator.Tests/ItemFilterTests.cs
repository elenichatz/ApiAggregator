using ApiAggregator.Api.Models;
using ApiAggregator.Tests.Fakes;

namespace ApiAggregator.Tests;

public class ItemFilterTests
{
    private static readonly AggregatedItem[] Items =
    [
        TestItems.Create("old news", category: "news", date: "2026-01-01", score: 10),
        TestItems.Create("new code", category: "code", date: "2026-03-01", score: 500),
        TestItems.Create("weather", category: "weather", date: "2026-02-01", score: null)
    ];

    [Fact]
    public void Apply_WithCategory_KeepsOnlyThatCategoryIgnoringCase()
    {
        var result = new ItemFilter(Category: "NEWS").Apply(Items);

        var item = Assert.Single(result);
        Assert.Equal("old news", item.Title);
    }

    [Fact]
    public void Apply_WithDateRange_KeepsOnlyItemsInsideRange()
    {
        var filter = new ItemFilter(From: DateTimeOffset.Parse("2026-01-15"), To: DateTimeOffset.Parse("2026-02-15"));

        var result = filter.Apply(Items);

        Assert.Equal(["weather"], result.Select(item => item.Title));
    }

    [Fact]
    public void Apply_WithDefaults_SortsByNewestFirst()
    {
        var result = new ItemFilter().Apply(Items);

        Assert.Equal(["new code", "weather", "old news"], result.Select(item => item.Title));
    }

    [Theory]
    [InlineData(SortOrder.Desc, new[] { "new code", "old news", "weather" })]
    [InlineData(SortOrder.Asc, new[] { "weather", "old news", "new code" })]
    public void Apply_SortByRelevance_OrdersByScore(SortOrder order, string[] expectedTitles)
    {
        var result = new ItemFilter(SortBy: SortBy.Relevance, Order: order).Apply(Items);

        Assert.Equal(expectedTitles, result.Select(item => item.Title));
    }
}