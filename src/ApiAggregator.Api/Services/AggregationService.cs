using ApiAggregator.Api.Clients;
using ApiAggregator.Api.Models;

namespace ApiAggregator.Api.Services;

public sealed class AggregationService(IEnumerable<IExternalApiClient> clients): IAggregationService
{
    public async Task<IReadOnlyList<AggregatedItem>> AggregateAsync(FetchRequest request,
        CancellationToken cancellationToken)
    {
        var tasks = clients.Select(client => client.FetchAsync(request, cancellationToken));
        var results = await Task.WhenAll(tasks);
        return results.SelectMany(items => items).ToList();
    }
}