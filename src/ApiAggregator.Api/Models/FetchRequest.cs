namespace ApiAggregator.Api.Models;

public sealed record FetchRequest(
    string Keyword, 
    string City);