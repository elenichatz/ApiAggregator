namespace ApiAggregator.Api.Models;

public sealed record ItemFilter(
    string? Category = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    SortBy SortBy = SortBy.Date,
    SortOrder Order = SortOrder.Desc)
{
    public IReadOnlyList<AggregatedItem> Apply(IEnumerable<AggregatedItem> items)
    {
        if (Category is not null)
        {
            items = items.Where(item => string.Equals(item.Category, Category, StringComparison.OrdinalIgnoreCase));
        }

        if (From is not null)
        {
            items = items.Where(item => item.Date >= From);
        }

        if (To is not null)
        {
            items = items.Where(item => item.Date <= To);
        }

        var sorted = (SortBy, Order) switch
        {
            (SortBy.Relevance, SortOrder.Asc) => items.OrderBy(item => item.Score),
            (SortBy.Relevance, SortOrder.Desc) => items.OrderByDescending(item => item.Score),
            (SortBy.Date, SortOrder.Asc) => items.OrderBy(item => item.Date),
            _ => items.OrderByDescending(item => item.Date)
        };

        return sorted.ToList();
    }
}