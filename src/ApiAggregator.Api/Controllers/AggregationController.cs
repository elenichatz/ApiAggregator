using ApiAggregator.Api.Models;
using ApiAggregator.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ApiAggregator.Api.Controllers;

[ApiController]
[Route("api/aggregate")]
public sealed class AggregationController(IAggregationService aggregationService) : ControllerBase
{
    /// <summary>Fetches data from all sources in parallel and returns the combined items.</summary>
    /// <remarks>At least one of keyword or city is required. Sources that cannot use the request are Skipped.</remarks>
    /// <param name="keyword">Search term for Hacker News and GitHub, e.g. dotnet.</param>
    /// <param name="city">City name for the weather forecast, e.g. Athens.</param>
    /// <param name="filter">Optional filtering and sorting.</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    /// <response code="200">The combined items and the status of every source.</response>
    /// <response code="400">Neither keyword nor city was given, or a parameter is invalid.</response>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
