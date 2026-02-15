namespace Nciems.Application.Common.Models;

public sealed class AuthResult
{
    public bool MfaRequired { get; init; }
    public string Message { get; init; } = string.Empty;
    public string? AccessToken { get; init; }
    public string? RefreshToken { get; init; }
    public DateTime? AccessTokenExpiresAtUtc { get; init; }
    public string UserName { get; init; } = string.Empty;
    public IReadOnlyCollection<string> Roles { get; init; } = [];
}
