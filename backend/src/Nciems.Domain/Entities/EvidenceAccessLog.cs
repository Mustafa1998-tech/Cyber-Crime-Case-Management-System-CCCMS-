using Nciems.Domain.Common;
using Nciems.Domain.Enums;

namespace Nciems.Domain.Entities;

public sealed class EvidenceAccessLog : BaseEntity
{
    public long EvidenceVersionId { get; set; }
    public long AccessedByUserId { get; set; }
    public EvidenceAccessType AccessType { get; set; } = EvidenceAccessType.View;
    public DateTime AccessedAtUtc { get; set; } = DateTime.UtcNow;

    public EvidenceVersion? EvidenceVersion { get; set; }
    public User? AccessedByUser { get; set; }
}
