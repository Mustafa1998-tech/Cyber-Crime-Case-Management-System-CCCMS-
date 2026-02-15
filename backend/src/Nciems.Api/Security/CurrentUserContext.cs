using System.Security.Claims;
using Nciems.Application.Interfaces;

namespace Nciems.Api.Security;

public sealed class CurrentUserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public long? UserId
    {
        get
        {
            var raw = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return long.TryParse(raw, out var parsed) ? parsed : null;
        }
    }

    public string? UserName => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Name);

    public IReadOnlyCollection<string> Roles =>
        httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(x => x.Value).ToArray()
        ?? Array.Empty<string>();

    public bool IsInRole(string role) =>
        httpContextAccessor.HttpContext?.User.IsInRole(role) ?? false;
}
