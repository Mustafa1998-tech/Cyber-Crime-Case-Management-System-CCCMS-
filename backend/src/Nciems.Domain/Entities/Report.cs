using Nciems.Domain.Common;
using Nciems.Domain.Enums;

namespace Nciems.Domain.Entities;

public sealed class Report : BaseEntity
{
    public long CaseId { get; set; }
    public ReportType ReportType { get; set; }
    public string Content { get; set; } = string.Empty;
    public long GeneratedByUserId { get; set; }
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public string? PdfPath { get; set; }
    public string DigitalSignature { get; set; } = string.Empty;
    public string QrPayload { get; set; } = string.Empty;

    public Case? Case { get; set; }
    public User? GeneratedByUser { get; set; }
}
