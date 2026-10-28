using System.Net.Http.Json;
using ApiAggregator.Api.Models;

namespace ApiAggregator.Api.Clients.GitHub;

public sealed class GitHubClient(HttpClient httpClient) : IExternalApiClient
{
    private const string SearchUrl = "https://api.github.com/search/repositories";
    
    public const string SourceName = "github";

    public string Name => SourceName;
    
    public bool CanHandle(FetchRequest request) => request.HasKeyword;

    public async Task<IReadOnlyList<AggregatedItem>> FetchAsync(FetchRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.HasKeyword)
        {
            return [];
        }
        
        var url = $"{SearchUrl}?q={Uri.EscapeDataString(request.Keyword)}&sort=stars&order=desc&per_page=10";

        var response = await httpClient.GetFromJsonAsync<GitHubSearchResponse>(url, cancellationToken);
        if (response is null)
        {
            return [];
        }

        return response.Items.Select(repo => new AggregatedItem(
                Source: Name,
                Category: "code",
                Title: repo.FullName,
                Url: repo.HtmlUrl,
                Date: repo.UpdatedAt,
                Score: repo.StargazersCount)).ToList();
    }
}