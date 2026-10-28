using ApiAggregator.Api.Clients.OpenMeteo;
using ApiAggregator.Api.Models;
using ApiAggregator.Tests.Fakes;

namespace ApiAggregator.Tests;

public class OpenMeteoClientTests
{
    private const string GeocodingJson = """
                                         { "results": [ { "name": "Athens", "latitude": 37.98, "longitude": 23.73 } ] }
                                         """;

    private const string ForecastJson = """
                                        {
                                          "utc_offset_seconds": 7200,
                                          "daily": {
                                            "time": ["2026-10-28", "2026-10-29"],
                                            "weather_code": [0, 61],
                                            "temperature_2m_max": [24.4, 20.0],
                                            "temperature_2m_min": [15.6, 14.0]
                                          }
                                        }
                                        """;

    [Fact]
    public async Task FetchAsync_KnownCity_ReturnsOneItemPerDay()
    {
        var handler = new StubHttpMessageHandler(request => StubHttpMessageHandler.Json(
            request.RequestUri!.Host.StartsWith("geocoding") ? GeocodingJson : ForecastJson));
        var client = new OpenMeteoClient(new HttpClient(handler));

        var items = await client.FetchAsync(new FetchRequest(null, "Athens"), CancellationToken.None);

        Assert.Equal(2, items.Count);
        Assert.Equal("Athens: 16-24°C, clear sky", items[0].Title);
        Assert.Equal("weather", items[0].Category);
        Assert.Equal(new DateTimeOffset(2026, 10, 28, 0, 0, 0, TimeSpan.FromHours(2)), items[0].Date);
        Assert.Contains("latitude=37.98", handler.Requests[1].Query);
    }

    [Fact]
    public async Task FetchAsync_UnknownCity_ReturnsNoItemsAndSkipsForecast()
    {
        var handler = new StubHttpMessageHandler(_ => StubHttpMessageHandler.Json("{}"));
        var client = new OpenMeteoClient(new HttpClient(handler));

        var items = await client.FetchAsync(new FetchRequest(null, "Atlantis"), CancellationToken.None);

        Assert.Empty(items);
        Assert.Single(handler.Requests);
    }
}