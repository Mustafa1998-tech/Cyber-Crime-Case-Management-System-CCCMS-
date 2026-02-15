using Nciems.Domain.Enums;

namespace Nciems.Application.Features.Complaints;

public sealed class ComplaintDto
{
    public long Id { get; init; }
    public string ComplainantName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string CrimeType { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public ComplaintStatus Status { get; init; }
    public long CreatedByUserId { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public long? CaseId { get; init; }
}
