namespace ApiAggregator.Api.Models;

public sealed record AggregatedItem(
    string Source,
    string Category,
    string Title,
    string? Url,
    DateTimeOffset Date,
    double? Score);