using System.Collections.Concurrent;
using ApiAggregator.Api.Models;

namespace ApiAggregator.Api.Stats;

public sealed class RequestStatsStore
{
    private readonly ConcurrentDictionary<string, Counter> counters = new();

    public void Record(string api, TimeSpan elapsed) => counters.GetOrAdd(api, _ => new Counter()).Add(elapsed);

    public IReadOnlyList<ApiStats> GetSnapshot() => counters
        .OrderBy(pair => pair.Key).Select(pair => pair.Value.ToStats(pair.Key)).ToList();

    private static double Average(double totalMilliseconds, long count) =>
        count == 0 ? 0 : Math.Round(totalMilliseconds / count, 1);

    private sealed class Counter
    {
        private readonly Lock gate = new();
        private readonly Bucket fast = new();
        private readonly Bucket average = new();
        private readonly Bucket slow = new();

        public void Add(TimeSpan elapsed)
        {
            var milliseconds = elapsed.TotalMilliseconds;
            var bucket = milliseconds switch
            {
                < 100 => fast,
                <= 200 => average,
                _ => slow
            };
            lock (gate)
            {
                bucket.Add(milliseconds);
            }
        }

        public ApiStats ToStats(string api)
        {
            lock (gate)
            {
                var count = fast.Count + average.Count + slow.Count;
                var totalMilliseconds = fast.TotalMilliseconds + average.TotalMilliseconds + slow.TotalMilliseconds;
                return new ApiStats(api, count, Average(totalMilliseconds, count),
                    fast.ToStats(), average.ToStats(), slow.ToStats());
            }
        }
    }

    private sealed class Bucket
    {
        public long Count { get; private set; }
        public double TotalMilliseconds { get; private set; }

        public void Add(double milliseconds)
        {
            Count++;
            TotalMilliseconds += milliseconds;
        }

        public BucketStats ToStats() => new(Count, Average(TotalMilliseconds, Count));
    }
}