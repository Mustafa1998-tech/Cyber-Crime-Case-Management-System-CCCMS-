using Nciems.Application.Common.Models;
using Nciems.Application.Interfaces;
using Nciems.Domain.Entities;

namespace Nciems.Application.Features.Auth;

internal static class AuthTokenIssuer
{
    public static async Task<AuthResult> IssueAsync(
        User user,
        IReadOnlyCollection<string> roles,
        ITokenService tokenService,
        IApplicationDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var accessToken = tokenService.GenerateAccessToken(user, roles);
        var refreshToken = tokenService.GenerateRefreshToken();

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshToken.RefreshTokenHash,
            ExpiresAtUtc = refreshToken.ExpiresAtUtc
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResult
        {
            MfaRequired = false,
            Message = "Authentication successful.",
            AccessToken = accessToken.AccessToken,
            RefreshToken = refreshToken.RefreshToken,
            AccessTokenExpiresAtUtc = accessToken.ExpiresAtUtc,
            UserName = user.UserName,
            Roles = roles
        };
    }
}
