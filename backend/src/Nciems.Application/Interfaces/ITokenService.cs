using Nciems.Application.Common.Models;
using Nciems.Domain.Entities;

namespace Nciems.Application.Interfaces;

public interface ITokenService
{
    AccessTokenResult GenerateAccessToken(User user, IReadOnlyCollection<string> roles);
    RefreshTokenResult GenerateRefreshToken();
    string GenerateOtpCode();
    string HashValue(string value);
}
