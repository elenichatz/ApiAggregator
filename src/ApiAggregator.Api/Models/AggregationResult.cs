namespace ApiAggregator.Api.Models;

public sealed record AggregationResult(IReadOnlyList<AggregatedItem> Items, IReadOnlyList<SourceStatus> Sources);