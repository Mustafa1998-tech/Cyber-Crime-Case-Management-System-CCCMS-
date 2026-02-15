using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nciems.Application.Features.Reports;
using Nciems.Domain.Enums;

namespace Nciems.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/reports")]
public sealed class ReportsController(IMediator mediator) : ControllerBase
{
    [HttpGet("case/{caseId:long}")]
    public async Task<IActionResult> GetByCase([FromRoute] long caseId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCaseReportsQuery(caseId), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReportRequest request, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(new CreateReportCommand(request.CaseId, request.ReportType, request.Content), cancellationToken);
        return Ok(id);
    }
}

public sealed class CreateReportRequest
{
    public long CaseId { get; set; }
    public ReportType ReportType { get; set; }
    public string Content { get; set; } = string.Empty;
}
