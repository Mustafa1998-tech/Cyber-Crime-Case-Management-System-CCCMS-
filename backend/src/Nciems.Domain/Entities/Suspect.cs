using Nciems.Domain.Common;

namespace Nciems.Domain.Entities;

public sealed class Suspect : BaseEntity
{
    public long CaseId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public string? Phone { get; set; }
    public string? IpAddress { get; set; }
    public string? AccountInfo { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public Case? Case { get; set; }
}
