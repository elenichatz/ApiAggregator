namespace ApiAggregator.Api.Models;

public sealed record ApiStats(string Api, long TotalRequests, double AverageResponseMs, long Fast, long Average, long Slow);