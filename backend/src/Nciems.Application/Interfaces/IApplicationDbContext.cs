using Microsoft.EntityFrameworkCore;
using Nciems.Domain.Entities;

namespace Nciems.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Complaint> Complaints { get; }
    DbSet<Case> Cases { get; }
    DbSet<CaseAssignment> CaseAssignments { get; }
    DbSet<Suspect> Suspects { get; }
    DbSet<Evidence> Evidence { get; }
    DbSet<EvidenceVersion> EvidenceVersions { get; }
    DbSet<EvidenceAccessLog> EvidenceAccessLogs { get; }
    DbSet<Report> Reports { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Notification> Notifications { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
