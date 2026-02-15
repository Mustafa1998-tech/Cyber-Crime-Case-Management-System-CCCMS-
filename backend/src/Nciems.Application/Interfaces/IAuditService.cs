namespace Nciems.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(
        string action,
        string entityName,
        string entityId,
        string details,
        long? userId,
        CancellationToken cancellationToken);
}
