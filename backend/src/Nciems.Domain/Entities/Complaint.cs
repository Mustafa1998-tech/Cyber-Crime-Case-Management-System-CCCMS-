using Nciems.Domain.Common;
using Nciems.Domain.Enums;

namespace Nciems.Domain.Entities;

public sealed class Complaint : BaseEntity
{
    public string ComplainantName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string CrimeType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ComplaintStatus Status { get; set; } = ComplaintStatus.New;
    public long CreatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public User? CreatedByUser { get; set; }
    public Case? Case { get; set; }
}
