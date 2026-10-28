using ApiAggregator.Api.Clients;
using ApiAggregator.Api.Models;

namespace ApiAggregator.Tests.Fakes;

internal sealed class FakeApiClient(string name, bool canHandle = true) : IExternalApiClient
{
    public Func<IReadOnlyList<AggregatedItem>> Respond { get; set; } = () => [];

    public int CallCount { get; private set; }

    public string Name => name;

    public bool CanHandle(FetchRequest request) => canHandle;

    public Task<IReadOnlyList<AggregatedItem>> FetchAsync(FetchRequest request, CancellationToken cancellationToken)
    {
        CallCount++;
        return Task.FromResult(Respond());
    }
}