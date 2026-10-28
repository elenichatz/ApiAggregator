using System.Net;
using ApiAggregator.Api.Clients.GitHub;
using ApiAggregator.Api.Models;
using ApiAggregator.Tests.Fakes;

namespace ApiAggregator.Tests;

public class GitHubClientTests
{
    private const string SearchJson = """
        {
          "items": [
            {
              "full_name": "dotnet/runtime",
              "html_url": "https://github.com/dotnet/runtime",
              "stargazers_count": 15000,
              "updated_at": "2026-10-01T12:00:00Z"
            }
          ]
        }
        """;

    [Fact]
    public async Task FetchAsync_MapsRepositoriesToItems()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json(SearchJson));
        var client = new GitHubClient(new HttpClient(handler));

        var items = await client.FetchAsync(new FetchRequest("c# tools", null), CancellationToken.None);

        var item = Assert.Single(items);
        Assert.Equal("github", item.Source);
        Assert.Equal("code", item.Category);
        Assert.Equal("dotnet/runtime", item.Title);
        Assert.Equal(15000, item.Score);
        Assert.Equal(DateTimeOffset.Parse("2026-10-01T12:00:00Z"), item.Date);
        Assert.Contains("q=c%23%20tools", Assert.Single(handler.Requests).AbsoluteUri);
    }

    [Fact]
    public async Task FetchAsync_ServerError_Throws()
    {
        var handler = new StubHttpMessageHandler(_ =>
            StubHttpMessageHandler.Json("{}", HttpStatusCode.ServiceUnavailable));
        var client = new GitHubClient(new HttpClient(handler));

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() =>
            client.FetchAsync(new FetchRequest("dotnet", null), CancellationToken.None));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, exception.StatusCode);
    }

    [Fact]
    public void CanHandle_WithoutKeyword_IsFalse()
    {
        var client = new GitHubClient(new HttpClient());

        Assert.False(client.CanHandle(new FetchRequest(null, "Athens")));
    }
}