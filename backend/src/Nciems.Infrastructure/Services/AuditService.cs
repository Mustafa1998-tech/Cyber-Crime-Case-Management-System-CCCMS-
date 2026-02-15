using Nciems.Application.Interfaces;
using Nciems.Domain.Entities;

namespace Nciems.Infrastructure.Services;

public sealed class AuditService(IApplicationDbContext dbContext) : IAuditService
{
    public async Task LogAsync(
        string action,
        string entityName,
        string entityId,
        string details,
        long? userId,
        CancellationToken cancellationToken)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            UserId = userId,
            TimestampUtc = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
