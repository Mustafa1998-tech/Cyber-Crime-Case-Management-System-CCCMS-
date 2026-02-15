using Nciems.Domain.Common;

namespace Nciems.Domain.Entities;

public sealed class Evidence : BaseEntity
{
    public long CaseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public long CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Case? Case { get; set; }
    public User? CreatedByUser { get; set; }
    public ICollection<EvidenceVersion> Versions { get; set; } = new List<EvidenceVersion>();
}
