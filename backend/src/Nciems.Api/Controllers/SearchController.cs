using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nciems.Application.Features.Search;

namespace Nciems.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/search")]
public sealed class SearchController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Global(
        [FromQuery] long? caseId,
        [FromQuery] string? hash,
        [FromQuery] string? ip,
        [FromQuery] string? phone,
        [FromQuery] string? suspectName,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GlobalSearchQuery(caseId, hash, ip, phone, suspectName), cancellationToken);
        return Ok(result);
    }
}
