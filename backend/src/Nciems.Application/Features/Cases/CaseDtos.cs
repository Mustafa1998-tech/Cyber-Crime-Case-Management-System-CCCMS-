using Nciems.Domain.Enums;

namespace Nciems.Application.Features.Cases;

public sealed class CaseListItemDto
{
    public long Id { get; init; }
    public long ComplaintId { get; init; }
    public string ComplaintCrimeType { get; init; } = string.Empty;
    public string ComplaintPhone { get; init; } = string.Empty;
    public CaseStatus Status { get; init; }
    public string Priority { get; init; } = string.Empty;
    public long? AssignedInvestigatorId { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}

public sealed class CaseDetailsDto
{
    public long Id { get; init; }
    public long ComplaintId { get; init; }
    public string ComplaintDescription { get; init; } = string.Empty;
    public string ComplainantName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public CaseStatus Status { get; init; }
    public string Priority { get; init; } = string.Empty;
    public long? AssignedInvestigatorId { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public DateTime? ClosedAtUtc { get; init; }
}
