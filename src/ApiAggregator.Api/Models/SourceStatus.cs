namespace ApiAggregator.Api.Models;

public sealed record SourceStatus(string Name, SourceState State, string? Error);