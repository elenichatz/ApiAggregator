using ApiAggregator.Api.Models;

namespace ApiAggregator.Api.Clients;

public interface IExternalApiClient
{
    string Name { get; }
    
    bool CanHandle(FetchRequest request);
    
    Task<IReadOnlyList<AggregatedItem>> FetchAsync(FetchRequest request, CancellationToken cancellationToken);
}