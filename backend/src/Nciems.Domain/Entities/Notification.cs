using Nciems.Domain.Common;

namespace Nciems.Domain.Entities;

public sealed class Notification : BaseEntity
{
    public long UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool Read { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
