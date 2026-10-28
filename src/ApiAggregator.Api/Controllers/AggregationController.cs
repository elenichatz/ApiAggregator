using ApiAggregator.Api.Models;
using ApiAggregator.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregator.Api.Controllers;

[ApiController]
[Route("api/aggregate")]
public sealed class AggregationController(IAggregationService aggregationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AggregatedItem>>> Get([FromQuery] string keyword, [FromQuery] string city,
        CancellationToken cancellationToken)
    {
        var items = await aggregationService.AggregateAsync(new FetchRequest(keyword, city), cancellationToken);

        return Ok(items);
    }
}
