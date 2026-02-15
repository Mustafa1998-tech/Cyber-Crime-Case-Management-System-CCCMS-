using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nciems.Application.Features.Suspects;

namespace Nciems.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/suspects")]
public sealed class SuspectsController(IMediator mediator) : ControllerBase
{
    [HttpGet("case/{caseId:long}")]
    public async Task<IActionResult> GetByCase([FromRoute] long caseId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCaseSuspectsQuery(caseId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddSuspectRequest request, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(new AddSuspectCommand(
            request.CaseId,
            request.Name,
            request.NationalId,
            request.Phone,
            request.IpAddress,
            request.AccountInfo,
            request.Notes), cancellationToken);

        return Ok(id);
    }
}

public sealed class AddSuspectRequest
{
    public long CaseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public string? Phone { get; set; }
    public string? IpAddress { get; set; }
    public string? AccountInfo { get; set; }
    public string? Notes { get; set; }
}
