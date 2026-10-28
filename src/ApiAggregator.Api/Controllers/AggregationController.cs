using ApiAggregator.Api.Models;
using ApiAggregator.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregator.Api.Controllers;

[ApiController]
[Route("api/aggregate")]
public sealed class AggregationController(IAggregationService aggregationService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AggregationResult>> Get([FromQuery] string? keyword, [FromQuery] string? city,
        [FromQuery] ItemFilter filter, CancellationToken cancellationToken)
    {
        var request = new FetchRequest(keyword, city);
        if (!request.HasKeyword && !request.HasCity)
        {
            ModelState.AddModelError(nameof(keyword), "Provide a keyword, a city, or both.");
            return ValidationProblem();
        }
        var result = await aggregationService.AggregateAsync(request, cancellationToken);

        return Ok(result with { Items = filter.Apply(result.Items) });
    }
}
