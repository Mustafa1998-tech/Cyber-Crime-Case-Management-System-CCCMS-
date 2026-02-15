using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Nciems.Application.Common.Models;
using Nciems.Application.Interfaces;
using Nciems.Domain.Entities;
using Nciems.Infrastructure.Options;

namespace Nciems.Infrastructure.Security;

public sealed class TokenService(IOptions<JwtOptions> options) : ITokenService
{
    private readonly JwtOptions jwtOptions = options.Value;

    public AccessTokenResult GenerateAccessToken(User user, IReadOnlyCollection<string> roles)
    {
        var now = DateTime.UtcNow;
        var expires = now.AddMinutes(jwtOptions.AccessTokenMinutes);
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Key));

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expires,
            Issuer = jwtOptions.Issuer,
            Audience = jwtOptions.Audience,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        };

        var handler = new JwtSecurityTokenHandler();
        var token = handler.CreateToken(descriptor);
        return new AccessTokenResult(handler.WriteToken(token), expires);
    }

    public RefreshTokenResult GenerateRefreshToken()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var hash = HashValue(token);
        var expiresAt = DateTime.UtcNow.AddDays(jwtOptions.RefreshTokenDays);
        return new RefreshTokenResult(token, hash, expiresAt);
    }

    public string GenerateOtpCode()
    {
        var value = RandomNumberGenerator.GetInt32(100000, 1000000);
        return value.ToString();
    }

    public string HashValue(string value)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
