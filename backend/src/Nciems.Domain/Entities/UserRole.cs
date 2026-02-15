using Nciems.Domain.Common;

namespace Nciems.Domain.Entities;

public sealed class UserRole : BaseEntity
{
    public long UserId { get; set; }
    public long RoleId { get; set; }

    public User? User { get; set; }
    public Role? Role { get; set; }
}
