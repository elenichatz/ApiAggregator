using System.Text.Json.Serialization;

namespace ApiAggregator.Api.Clients.HackerNews;

internal sealed record HackerNewsSearchResponse(IReadOnlyList<HackerNewsHit> Hits);

internal sealed record HackerNewsHit(string ObjectId, string Title, string? Url, int? Points, [property: JsonPropertyName("created_at")] DateTimeOffset CreatedAt);
