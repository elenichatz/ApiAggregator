using ApiAggregator.Api.Models;
using ApiAggregator.Api.Stats;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregator.Api.Controllers;

[ApiController]
[Route("api/stats")]
public sealed class StatsController(RequestStatsStore store) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<ApiStats>> Get() => Ok(store.GetSnapshot());
}