using System.Diagnostics.CodeAnalysis;

namespace ApiAggregator.Api.Models;

public sealed record FetchRequest(string? Keyword, string? City)
{
    [MemberNotNullWhen(true, nameof(Keyword))]
    public bool HasKeyword => !string.IsNullOrWhiteSpace(Keyword);

    [MemberNotNullWhen(true, nameof(City))]
    public bool HasCity => !string.IsNullOrWhiteSpace(City);
}