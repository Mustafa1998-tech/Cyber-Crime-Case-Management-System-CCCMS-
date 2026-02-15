using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nciems.Application.Features.Complaints;
using Nciems.Domain.Enums;

namespace Nciems.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/complaints")]
public sealed class ComplaintsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] ComplaintStatus? status, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetComplaintsQuery(status), cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateComplaintRequest request, CancellationToken cancellationToken)
    {
        var id = await mediator.Send(
            new CreateComplaintCommand(request.ComplainantName, request.Phone, request.CrimeType, request.Description),
            cancellationToken);

        return Ok(id);
    }

    [HttpPost("{complaintId:long}/review")]
    public async Task<IActionResult> Review(
        [FromRoute] long complaintId,
        [FromBody] ReviewComplaintRequest request,
        CancellationToken cancellationToken)
    {
        var caseId = await mediator.Send(new ReviewComplaintCommand(
            complaintId,
            request.Approved,
            request.AssignedInvestigatorId,
            request.Priority,
            request.RejectionReason), cancellationToken);

        return Ok(new { CaseId = caseId });
    }
}

public sealed class CreateComplaintRequest
{
    public string ComplainantName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string CrimeType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public sealed class ReviewComplaintRequest
{
    public bool Approved { get; set; }
    public long? AssignedInvestigatorId { get; set; }
    public string? Priority { get; set; }
    public string? RejectionReason { get; set; }
}
