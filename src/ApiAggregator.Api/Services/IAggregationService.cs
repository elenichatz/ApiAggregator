using ApiAggregator.Api.Models;

namespace ApiAggregator.Api.Services;

public interface IAggregationService
{
    Task<IReadOnlyList<AggregatedItem>> AggregateAsync(FetchRequest request, CancellationToken cancellationToken);
}

