using Nciems.Domain.Common;

namespace Nciems.Domain.Entities;

public sealed class CaseAssignment : BaseEntity
{
    public long CaseId { get; set; }
    public long InvestigatorId { get; set; }
    public long AssignedByUserId { get; set; }
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;

    public Case? Case { get; set; }
    public User? Investigator { get; set; }
    public User? AssignedByUser { get; set; }
}
