using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nciems.Application.Features.Cases;
using Nciems.Domain.Enums;

namespace Nciems.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/cases")]
public sealed class CasesController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] CaseStatus? status, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCasesQuery(status), cancellationToken);
        return Ok(result);
    }

    [HttpGet("{caseId:long}")]
    public async Task<IActionResult> GetById([FromRoute] long caseId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCaseByIdQuery(caseId), cancellationToken);
        return Ok(result);
    }

    [HttpPost("{caseId:long}/assign")]
    public async Task<IActionResult> Assign(
        [FromRoute] long caseId,
        [FromBody] AssignInvestigatorRequest request,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new AssignInvestigatorCommand(caseId, request.InvestigatorId), cancellationToken);
        return NoContent();
    }

    [HttpPut("{caseId:long}/status")]
    public async Task<IActionResult> ChangeStatus(
        [FromRoute] long caseId,
        [FromBody] ChangeCaseStatusRequest request,
        CancellationToken cancellationToken)
    {
        await mediator.Send(new ChangeCaseStatusCommand(caseId, request.NewStatus), cancellationToken);
        return NoContent();
    }
}

public sealed class AssignInvestigatorRequest
{
    public long InvestigatorId { get; set; }
}

public sealed class ChangeCaseStatusRequest
{
    public CaseStatus NewStatus { get; set; }
}
