using ApiAggregator.Api.Models;
using ApiAggregator.Api.Stats;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregator.Api.Controllers;


[ApiController]
[Route("api/stats")]
public sealed class StatsController(RequestStatsStore store) : ControllerBase
{
    /// <summary>Request statistics per external API host since the service started.</summary>
    /// <remarks>Buckets: fast under 100 ms, average 100-200 ms, slow over 200 ms. Cached responses are not counted.</remarks>
    [HttpGet]
    public ActionResult<IReadOnlyList<ApiStats>> Get() => Ok(store.GetSnapshot());
}