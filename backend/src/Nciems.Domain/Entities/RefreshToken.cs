using Nciems.Domain.Common;

namespace Nciems.Domain.Entities;

public sealed class RefreshToken : BaseEntity
{
    public long UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAtUtc { get; set; }
    public bool IsRevoked => RevokedAtUtc.HasValue;

    public User? User { get; set; }
}
