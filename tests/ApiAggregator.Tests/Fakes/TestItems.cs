using ApiAggregator.Api.Models;

namespace ApiAggregator.Tests.Fakes;

internal static class TestItems
{
    public static AggregatedItem Create(string title, string category = "news", string date = "2026-01-01",
        double? score = null) =>
        new(Source: "test", Category: category, Title: title, Url: null, Date: DateTimeOffset.Parse(date), Score: score);
}