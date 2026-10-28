using System.Text.Json.Serialization;

namespace ApiAggregator.Api.Models;

[JsonConverter(typeof(JsonStringEnumConverter<SourceState>))]
public enum SourceState
{
    Succeeded,
    Failed,
    Skipped
}