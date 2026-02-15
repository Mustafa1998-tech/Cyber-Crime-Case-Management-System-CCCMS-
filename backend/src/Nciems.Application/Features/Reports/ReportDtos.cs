using Nciems.Domain.Enums;

namespace Nciems.Application.Features.Reports;

public sealed class ReportDto
{
    public long Id { get; init; }
    public long CaseId { get; init; }
    public ReportType ReportType { get; init; }
    public string Content { get; init; } = string.Empty;
    public long GeneratedByUserId { get; init; }
    public DateTime GeneratedAtUtc { get; init; }
    public string DigitalSignature { get; init; } = string.Empty;
    public string QrPayload { get; init; } = string.Empty;
}
