using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nciems.Application.Common.Exceptions;
using Nciems.Application.Common.Models;
using Nciems.Application.Common.Validation;
using Nciems.Application.Interfaces;
using Nciems.Domain.Entities;

namespace Nciems.Application.Features.Auth;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<AuthResult>;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .MaximumLength(512)
            .MustBeSafeText(nameof(RefreshTokenCommand.RefreshToken), checkSqlPatterns: false);
    }
}

public sealed class RefreshTokenCommandHandler(
    IApplicationDbContext dbContext,
    ITokenService tokenService,
    IAuditService auditService) : IRequestHandler<RefreshTokenCommand, AuthResult>
{
    public async Task<AuthResult> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = tokenService.HashValue(request.RefreshToken);

        var refreshToken = await dbContext.RefreshTokens
            .Include(x => x.User)
            .ThenInclude(x => x!.UserRoles)
            .ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (refreshToken?.User is null || refreshToken.IsRevoked || refreshToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new ForbiddenException("Invalid refresh token.");
        }

        refreshToken.RevokedAtUtc = DateTime.UtcNow;

        var user = refreshToken.User;
        var roles = user.UserRoles.Select(x => x.Role!.Name).ToArray();
        var result = await AuthTokenIssuer.IssueAsync(user, roles, tokenService, dbContext, cancellationToken);

        await auditService.LogAsync("RefreshTokenSuccess", nameof(User), user.Id.ToString(), "token_rotated", user.Id, cancellationToken);
        return result;
    }
}
