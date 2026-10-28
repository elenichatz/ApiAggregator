using System.Text.Json.Serialization;

namespace ApiAggregator.Api.Models;

[JsonConverter(typeof(JsonStringEnumConverter<SortOrder>))]
public enum SortOrder
{
    Asc,
    Desc
}