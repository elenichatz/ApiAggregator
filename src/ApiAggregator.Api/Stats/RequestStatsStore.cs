using System.Collections.Concurrent;
using ApiAggregator.Api.Models;

namespace ApiAggregator.Api.Stats;

public sealed class RequestStatsStore
{
    private readonly ConcurrentDictionary<string, Counter> counters = new();

    public void Record(string api, TimeSpan elapsed) => counters.GetOrAdd(api, _ => new Counter()).Add(elapsed);

    public IReadOnlyList<ApiStats> GetSnapshot() => counters
        .OrderBy(pair => pair.Key).Select(pair => pair.Value.ToStats(pair.Key)).ToList();

    private sealed class Counter
    {
        private readonly Lock gate = new();
        private long count;
        private double totalMilliseconds;
        private long fast;
        private long average;
        private long slow;

        public void Add(TimeSpan elapsed)
        {
            var milliseconds = elapsed.TotalMilliseconds;
            lock (gate)
            {
                count++;
                totalMilliseconds += milliseconds;
                if (milliseconds < 100)
                {
                    fast++;
                }
                else if (milliseconds <= 200)
                {
                    average++;
                }
                else
                {
                    slow++;
                }
            }
        }

        public ApiStats ToStats(string api)
        {
            lock (gate)
            {
                return new ApiStats(api, count, Math.Round(totalMilliseconds / count, 1), fast, average, slow);
            }
        }
    }
}