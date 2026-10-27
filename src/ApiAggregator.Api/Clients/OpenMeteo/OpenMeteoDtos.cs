using System.Text.Json.Serialization;

namespace ApiAggregator.Api.Clients.OpenMeteo;

internal sealed record GeoCodingResponse(IReadOnlyList<GeoLocation>? Results);

internal sealed record GeoLocation(string Name, double Latitude, double Longitude);

internal sealed record Forecastresponse([property: JsonPropertyName("utc_offset_seconds")] int UtcOffsetSeconds, DailyForecast Daily);

internal sealed record DailyForecast(IReadOnlyList<DateOnly> Time, [property: JsonPropertyName("weather_code")] IReadOnlyList<int> WeatherCode, [property: JsonPropertyName("temperature_2m_max")] IReadOnlyList<double> TemperatureMax,[property: JsonPropertyName("temperature_2m_min")] IReadOnlyList<double> TemperatureMin);

