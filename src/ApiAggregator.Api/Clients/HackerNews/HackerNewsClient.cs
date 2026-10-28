using System.Net.Http.Json;
using ApiAggregator.Api.Models;

namespace ApiAggregator.Api.Clients.HackerNews;

public sealed class HackerNewsClient(HttpClient httpClient) : IExternalApiClient
{
    private const string SearchUrl = "https://hn.algolia.com/api/v1/search";

    public string Name => "hackernews";

    public async Task<IReadOnlyList<AggregatedItem>> FetchAsync(FetchRequest request,
        CancellationToken cancellationToken)
    {
        var url = $"{SearchUrl}?query={Uri.EscapeDataString(request.Keyword)}&tags=story&hitsPerPage=10";
        
        var response = await httpClient.GetFromJsonAsync<HackerNewsSearchResponse>(url, cancellationToken);
        if (response == null)
        {
            return [];
        }
        return response.Hits.Select(hit => new AggregatedItem(
                Source: Name,
                Category: "news",
                Title: hit.Title,
                Url: hit.Url ?? $"https://news.ycombinator.com/item?id={hit.ObjectId}",
                Date: hit.CreatedAt,
                Score: hit.Points)).ToList();
    }
}