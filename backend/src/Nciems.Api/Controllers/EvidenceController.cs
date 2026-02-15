using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nciems.Application.Features.Evidence;
using Nciems.Domain.Enums;

namespace Nciems.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/evidence")]
public sealed class EvidenceController(IMediator mediator) : ControllerBase
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf",
        ".png",
        ".jpg",
        ".jpeg",
        ".txt",
        ".csv",
        ".json",
        ".zip",
        ".7z",
        ".mp3",
        ".wav",
        ".mp4"
    };

    [HttpGet("case/{caseId:long}")]
    public async Task<IActionResult> GetByCase([FromRoute] long caseId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCaseEvidenceQuery(caseId), cancellationToken);
        return Ok(result);
    }

    [RequestSizeLimit(50_000_000)]
    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] UploadEvidenceRequest request, CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest("File is required.");
        }

        if (request.File.Length > 50_000_000)
        {
            return BadRequest("File exceeds maximum allowed size (50 MB).");
        }

        var extension = Path.GetExtension(request.File.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            return BadRequest("File type is not allowed.");
        }

        await using var stream = new MemoryStream();
        await request.File.CopyToAsync(stream, cancellationToken);

        var result = await mediator.Send(new UploadEvidenceCommand(
            request.CaseId,
            request.ExistingEvidenceId,
            request.Title,
            request.Description,
            request.File.FileName,
            request.File.ContentType,
            request.DeviceInfo ?? "Unknown",
            stream.ToArray()), cancellationToken);

        return Ok(result);
    }

    [HttpGet("versions/{evidenceVersionId:long}/download")]
    public async Task<IActionResult> Download(
        [FromRoute] long evidenceVersionId,
        [FromQuery] EvidenceAccessType accessType = EvidenceAccessType.Download,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new DownloadEvidenceVersionQuery(evidenceVersionId, accessType), cancellationToken);
        Response.Headers.Append("X-Evidence-SHA256", result.Sha256Hash);
        Response.Headers.Append("X-Evidence-MD5", result.Md5Hash);
        return File(result.FileBytes, result.MimeType, result.FileName);
    }
}

public sealed class UploadEvidenceRequest
{
    public long CaseId { get; set; }
    public long? ExistingEvidenceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? DeviceInfo { get; set; }
    public IFormFile? File { get; set; }
}
