namespace Nciems.Application.Interfaces;

public interface IUserContext
{
    long? UserId { get; }
    string? UserName { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool IsInRole(string role);
}
