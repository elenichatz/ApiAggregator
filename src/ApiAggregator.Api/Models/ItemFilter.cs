namespace ApiAggregator.Api.Models;

/// <summary>Optional filtering and sorting applied to the aggregated items.</summary>
/// <param name="Category">Keep only this category: news, code or weather (case-insensitive).</param>
/// <param name="From">Keep items on or after this date, e.g. 2026-10-01.</param>
/// <param name="To">Keep items on or before this date.</param>
/// <param name="SortBy">Date (default) or Relevance (score: HN points / GitHub stars).</param>
/// <param name="Order">Desc (default) or Asc.</param>
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