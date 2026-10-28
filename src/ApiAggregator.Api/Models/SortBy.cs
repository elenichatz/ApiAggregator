using System.Text.Json.Serialization;

namespace ApiAggregator.Api.Models;

[JsonConverter(typeof(JsonStringEnumConverter<SortBy>))]
public enum SortBy
{
    Date,
    Relevance
}