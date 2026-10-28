# API Aggregator

An ASP.NET Core (.NET 10) service that queries several public APIs **in parallel** and returns their data through **one unified endpoint**, with filtering, sorting, resilience, caching and per-API request statistics.

| Source | API | Used for | Needs |
|---|---|---|---|
| `openmeteo` | [Open-Meteo](https://open-meteo.com/) geocoding + forecast | 3-day weather forecast for a city | `city` |
| `hackernews` | [HN Algolia search](https://hn.algolia.com/api) | news stories matching a keyword | `keyword` |
| `github` | [GitHub repository search](https://docs.github.com/en/rest/search/search#search-repositories) | top repositories matching a keyword | `keyword` |

None of the APIs require an API key.

## Getting started

**Requirements:** [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

```bash
git clone <repository-url>
cd ApiAggregator
dotnet run --project src/ApiAggregator.Api --launch-profile http
```

Then open **http://localhost:5088/swagger** to try the endpoints in the browser.
Swagger UI is enabled in the Development environment only.

Run the tests:

```bash
dotnet test
```

`src/ApiAggregator.Api/ApiAggregator.Api.http` contains ready-made requests for the Rider / Visual Studio / VS Code HTTP client.

## Endpoints

### `GET /api/aggregate`

Fetches data from all sources that can handle the request, in parallel, and returns the combined items plus the status of each source.

**Query parameters**

| Parameter | Type | Default | Description |
|---|---|---|---|
| `keyword` | string | – | Search term for Hacker News and GitHub |
| `city` | string | – | City name for the weather forecast |
| `category` | string | – | Keep only one category: `news`, `code` or `weather` (case-insensitive) |
| `from` | date-time | – | Keep items on or after this date, e.g. `2026-10-01` |
| `to` | date-time | – | Keep items on or before this date |
| `sortBy` | `Date` \| `Relevance` | `Date` | Sort by item date, or by score (HN points / GitHub stars) |
| `order` | `Asc` \| `Desc` | `Desc` | Sort direction |

At least one of `keyword` or `city` is required. A source that cannot use the request (for example GitHub when only `city` is given) is reported as `Skipped` and is not called.

**Example:** `GET /api/aggregate?keyword=dotnet&city=Athens&sortBy=relevance`

```json
{
  "items": [
    {
      "source": "github",
      "category": "code",
      "title": "jellyfin/jellyfin",
      "url": "https://github.com/jellyfin/jellyfin",
      "date": "2026-10-28T16:50:17+00:00",
      "score": 58741
    },
    {
      "source": "openmeteo",
      "category": "weather",
      "title": "Athens: 10-16°C, rain showers",
      "url": null,
      "date": "2026-10-28T00:00:00+02:00",
      "score": null
    }
  ],
  "sources": [
    { "name": "openmeteo", "state": "Succeeded", "error": null },
    { "name": "hackernews", "state": "Succeeded", "error": null },
    { "name": "github", "state": "Succeeded", "error": null }
  ]
}
```

**Source states**

| State | Meaning |
|---|---|
| `Succeeded` | Fresh data fetched from the API |
| `Cached` | Served from the cache without calling the API |
| `Stale` | The API failed; older cached data is returned instead. `error` says why |
| `Failed` | The API failed and there was nothing cached. Its items are missing, the other sources still return theirs |
| `Skipped` | The source does not apply to this request |

`error` is a short, safe description such as `HTTP 503`, `Timed out` or `Circuit open`. Full exception details are only written to the server log.

**Errors**

- `400 Bad Request` (ProblemDetails) when neither `keyword` nor `city` is given, or when a parameter has an invalid value (e.g. `sortBy=popularity`).

### `GET /api/stats`

Request statistics for every external API host called since the service started.

```json
[
  {
    "api": "api.github.com",
    "totalRequests": 12,
    "averageResponseMs": 451.3,
    "fast": 0,
    "average": 3,
    "slow": 9
  }
]
```

Each outgoing HTTP request is put into a performance bucket: `fast` < 100 ms, `average` 100–200 ms, `slow` > 200 ms.
Every attempt counts, retries included; cache hits make no request and are not counted.
Open-Meteo appears as two hosts (`geocoding-api.open-meteo.com` and `api.open-meteo.com`) because the client makes a geocoding call and a forecast call.

## Configuration

`appsettings.json`:

```json
"Cache": {
  "FreshFor": "00:05:00",
  "StaleFor": "01:00:00"
}
```

| Setting | Meaning |
|---|---|
| `Cache:FreshFor` | How long cached data is served without calling the API |
| `Cache:StaleFor` | How long cached data is kept as a fallback for when the API fails |

The values can be overridden per environment (`appsettings.Development.json`) or with environment variables, e.g. `Cache__FreshFor=00:10:00`.

## Architecture

```
GET /api/aggregate
  AggregationController ── validates input, applies ItemFilter (filter + sort)
    AggregationService ── runs all sources in parallel (Task.WhenAll), cache + fallback per source
      IExternalApiClient ── OpenMeteoClient · HackerNewsClient · GitHubClient
        HttpClient (IHttpClientFactory)
          ├─ Standard resilience handler (Polly): timeout · retry · circuit breaker
          └─ RequestStatsHandler: times every request → RequestStatsStore ── GET /api/stats
```

```
src/ApiAggregator.Api
  Clients/      one folder per external API: client + DTOs
  Controllers/  AggregationController, StatsController
  Models/       public request/response types, ItemFilter
  Services/     AggregationService, CacheOptions
  Stats/        RequestStatsHandler, RequestStatsStore
tests/ApiAggregator.Tests
  Fakes/        fake API client, stub HTTP handler, test data
```

### Design decisions

- **One interface per source.** Every API implements `IExternalApiClient` (`Name`, `CanHandle`, `FetchAsync`) and maps its own response to the common `AggregatedItem`. Adding a fourth API means one new class and one line in `Program.cs`; the service and controller do not change.
- **Parallel calls.** `Task.WhenAll` starts all sources at once, so the response takes as long as the slowest source, not the sum of all of them.
- **Typed clients via `IHttpClientFactory`.** Connections are pooled and reused, which avoids socket exhaustion and DNS problems from creating `HttpClient` instances by hand. GitHub's client sets the `User-Agent` header the GitHub API requires.
- **Resilience at the transport layer.** `AddStandardResilienceHandler` (Polly) applies to every client: 3 s per attempt, retries with exponential backoff and jitter for transient errors (5xx, 408, 429, network), a circuit breaker, and 10 s in total.
- **Fallback at the application layer.** Each source is wrapped in its own `try/catch`, so one failing API never fails the whole request. If the client cancels the request, the cancellation is not reported as a source failure.
- **Cache per source, before filtering.** Each source's raw items are cached under `source|keyword|city`, with keyword and city trimmed and lower-cased. Only successful results are cached, so a failed source is retried on the next request. Filtering and sorting run after the cache, so `?category=news` and `?sortBy=date` reuse the same cached data. The cache uses the **stale-if-error** pattern and is limited to 1000 entries so that random keywords cannot fill the memory.
- **Thread-safe statistics.** A `DelegatingHandler` sits inside the resilience handler and times each attempt. The store uses a `ConcurrentDictionary` with a lock per API, so counts and totals always stay consistent with each other.
- **Testable time.** Cache expiry and request timing use `TimeProvider`, so tests can move a fake clock forward instead of waiting.

## Tests

23 xUnit tests, none of which touch the network:

| Area | What is covered |
|---|---|
| `ItemFilter` | category filter (case-insensitive), date range, default sort, relevance asc/desc incl. items without score |
| `AggregationService` | combining sources, partial results on failure, skipped sources, cache hit (with key normalisation), refresh after expiry, stale fallback, cancellation |
| `RequestStatsStore` | bucket boundaries (100 / 200 ms), average, per-API separation, 10,000 concurrent writes |
| `RequestStatsHandler` | timing with a fake clock, failed requests are still counted |
| `GitHubClient`, `OpenMeteoClient` | JSON → `AggregatedItem` mapping, URL encoding, culture-invariant coordinates, HTTP errors, unknown city |

The fakes used are a fake `IExternalApiClient`, a stub `HttpMessageHandler` that returns canned JSON, and `FakeTimeProvider`.

## Known limitations and possible improvements

- **Relevance across sources:** GitHub stars and HN points are on different scales, so `sortBy=relevance` tends to rank GitHub first. Normalising scores per source would fix this.
- **Slowest source sets the pace:** an uncached request waits for the slowest API, up to the 10 s total timeout.
- **Cache stampede:** two identical requests arriving together while the cache is empty both call the API. `HybridCache` (.NET 9+) deduplicates these.
- **Single instance:** the cache and the statistics live in process memory, are lost on restart and are not shared between instances. With several instances you would use Redis for the cache and OpenTelemetry metrics for the statistics.
- **Cache key:** weather is cached per `keyword + city`, so the same city with a different keyword is fetched again.
- **GitHub rate limit:** unauthenticated search allows 10 requests per minute. Sending a token would raise the limit.
- **Not implemented (optional parts of the assignment):** JWT authentication, and a background service that logs performance anomalies.
