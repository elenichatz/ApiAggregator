using ApiAggregator.Api.Models;

namespace ApiAggregator.Api.Clients;

public interface IExternalApiClient
{
    string Name { get; }
    
    Task<IReadOnlyList<AggregatedItem>> FetchAsync(FetchRequest request, CancellationToken cancellationToken);
}