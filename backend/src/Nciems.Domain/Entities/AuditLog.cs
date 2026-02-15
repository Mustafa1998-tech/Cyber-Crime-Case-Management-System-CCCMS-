using Nciems.Domain.Common;

namespace Nciems.Domain.Entities;

public sealed class AuditLog : BaseEntity
{
    public long? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string Details { get; set; } = string.Empty;

    public User? User { get; set; }
}
