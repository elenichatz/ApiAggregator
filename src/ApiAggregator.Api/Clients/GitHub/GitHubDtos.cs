using System.Text.Json.Serialization;

namespace ApiAggregator.Api.Clients.GitHub;

internal sealed record GitHubSearchResponse(IReadOnlyList<GitHubRepository> Items);

internal sealed record GitHubRepository([property: JsonPropertyName("full_name")] string FullName, [property: JsonPropertyName("html_url")] string HtmlUrl,
[property: JsonPropertyName("stargazers_count")] int StargazersCount, [property: JsonPropertyName("updated_at")] DateTimeOffset UpdatedAt);
