using Nciems.Domain.Common;
using Nciems.Domain.Enums;

namespace Nciems.Domain.Entities;

public sealed class Case : BaseEntity
{
    public long ComplaintId { get; set; }
    public long? AssignedInvestigatorId { get; set; }
    public CaseStatus Status { get; set; } = CaseStatus.New;
    public string Priority { get; set; } = "Medium";
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAtUtc { get; set; }

    public Complaint? Complaint { get; set; }
    public User? AssignedInvestigator { get; set; }
    public ICollection<CaseAssignment> Assignments { get; set; } = new List<CaseAssignment>();
    public ICollection<Suspect> Suspects { get; set; } = new List<Suspect>();
    public ICollection<Evidence> EvidenceItems { get; set; } = new List<Evidence>();
    public ICollection<Report> Reports { get; set; } = new List<Report>();
}
