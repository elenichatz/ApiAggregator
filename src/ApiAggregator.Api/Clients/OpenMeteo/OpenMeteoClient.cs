using System.Net;
using System.Net.Http.Json;
using ApiAggregator.Api.Models;

namespace ApiAggregator.Api.Clients.OpenMeteo;

public sealed class OpenMeteoClient(HttpClient httpClient) : IExternalApiClient
{
    private const string GeocodingUrl = "https://geocoding-api.open-meteo.com/v1/search";
    private const string ForecastUrl = "https://api.open-meteo.com/v1/forecast";

    public string Name => "openmeteo";

    public async Task<IReadOnlyList<AggregatedItem>> FetchAsync(FetchRequest request,
        CancellationToken cancellationToken)
    {
        var location = await FindLocationAsync(request.City, cancellationToken);
        if (location is null)
        {
            return [];
        }

        var url = FormattableString.Invariant(
            $"{ForecastUrl}?latitude={location.Latitude}&longitude={location.Longitude}&daily=weather_code,temperature_2m_max,temperature_2m_min&timezone=auto&forecast_days=3");

        var forecast = await httpClient.GetFromJsonAsync<ForecastResponse>(url, cancellationToken);
        if (forecast is null)
        {
            return [];
        }

        var offset = TimeSpan.FromSeconds(forecast.UtcOffsetSeconds);
        var daily = forecast.Daily;

        return daily.Time.Select((day, i) => new AggregatedItem(
                Source: Name,
                Category: "weather",
                Title:
                $"{location.Name}: {daily.TemperatureMin[i]:0}-{daily.TemperatureMax[i]:0}°C, {Describe(daily.WeatherCode[i])}",
                Url: null,
                Date: new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), offset),
                Score: null))
            .ToList();
    }

    private async Task<GeoLocation?> FindLocationAsync(string city, CancellationToken cancellationToken)
    {
        var url = $"{GeocodingUrl}?name={Uri.EscapeDataString(city)}&count=1";
        var response = await httpClient.GetFromJsonAsync<GeocodingResponse>(url, cancellationToken);
        return response?.Results?.FirstOrDefault();
    }

    private static string Describe(int weatherCode) => weatherCode switch
    {
        0 => "clear sky",
        1 or 2 or 3 => "partly cloudy",
        45 or 48 => "fog",
        >= 51 and <= 67 => "rain",
        >= 71 and <= 77 => "snow",
        >= 80 and <= 82 => "rain showers",
        >= 85 and <= 86 => "snow showers",
        >= 95 => "thunderstorm",
        _ => "unknown"
    };
}

