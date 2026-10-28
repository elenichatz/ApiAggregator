namespace ApiAggregator.Api.Services;

public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    public TimeSpan FreshFor { get; init; } = TimeSpan.FromMinutes(5);

    public TimeSpan StaleFor { get; init; } = TimeSpan.FromHours(1);
}