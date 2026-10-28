using System.ComponentModel.DataAnnotations;
using ApiAggregator.Api.Models;
using ApiAggregator.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregator.Api.Controllers;

[ApiController]
[Route("api/aggregate")]
public sealed class AggregationController(IAggregationService aggregationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AggregationResult>> Get([FromQuery, Required] string keyword, [FromQuery, Required] string city,
        [FromQuery] ItemFilter filter, CancellationToken cancellationToken)
    {
        var result = await aggregationService.AggregateAsync(new FetchRequest(keyword, city), cancellationToken);

        return Ok(result with { Items = filter.Apply(result.Items) });
    }
}
