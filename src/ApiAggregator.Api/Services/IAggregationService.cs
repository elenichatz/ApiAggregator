using ApiAggregator.Api.Models;

namespace ApiAggregator.Api.Services;

public interface IAggregationService
{
    Task<AggregationResult> AggregateAsync(FetchRequest request, CancellationToken cancellationToken);
}

