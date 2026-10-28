namespace ApiAggregator.Api.Models;

public sealed record ApiStats(
    string Api,
    long TotalRequests,
    double AverageResponseMs,
    BucketStats Fast,
    BucketStats Average,
    BucketStats Slow);
    
public sealed record BucketStats(long Requests, double AverageResponseMs);