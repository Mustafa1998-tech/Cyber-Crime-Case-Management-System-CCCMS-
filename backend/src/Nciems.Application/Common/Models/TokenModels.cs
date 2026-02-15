namespace Nciems.Application.Common.Models;

public sealed record AccessTokenResult(string AccessToken, DateTime ExpiresAtUtc);
public sealed record RefreshTokenResult(string RefreshToken, string RefreshTokenHash, DateTime ExpiresAtUtc);
