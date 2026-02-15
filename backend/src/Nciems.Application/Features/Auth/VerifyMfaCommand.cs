using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nciems.Application.Common.Exceptions;
using Nciems.Application.Common.Models;
using Nciems.Application.Common.Validation;
using Nciems.Application.Interfaces;
using Nciems.Domain.Entities;

namespace Nciems.Application.Features.Auth;

public sealed record VerifyMfaCommand(string UserName, string OtpCode) : IRequest<AuthResult>;

public sealed class VerifyMfaCommandValidator : AbstractValidator<VerifyMfaCommand>
{
    public VerifyMfaCommandValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty()
            .MaximumLength(100)
            .MustBeSafeText(nameof(VerifyMfaCommand.UserName));

        RuleFor(x => x.OtpCode)
            .NotEmpty()
            .Length(6)
            .Matches(@"^\d{6}$")
            .WithMessage("OTP code must contain exactly 6 digits.");
    }
}

public sealed class VerifyMfaCommandHandler(
    IApplicationDbContext dbContext,
    ITokenService tokenService,
    IAuditService auditService) : IRequestHandler<VerifyMfaCommand, AuthResult>
{
    public async Task<AuthResult> Handle(VerifyMfaCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.UserName == request.UserName.Trim(), cancellationToken);

        if (user is null)
        {
            throw new ForbiddenException("Invalid MFA verification request.");
        }

        if (string.IsNullOrWhiteSpace(user.PendingMfaCodeHash) ||
            !user.PendingMfaCodeExpiresAtUtc.HasValue ||
            user.PendingMfaCodeExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new ForbiddenException("MFA code expired. Please login again.");
        }

        if (tokenService.HashValue(request.OtpCode) != user.PendingMfaCodeHash)
        {
            await auditService.LogAsync("MfaFailed", nameof(User), user.Id.ToString(), "invalid_otp", user.Id, cancellationToken);
            throw new ForbiddenException("Invalid MFA code.");
        }

        user.PendingMfaCodeHash = null;
        user.PendingMfaCodeExpiresAtUtc = null;

        var roles = user.UserRoles.Select(x => x.Role!.Name).ToArray();
        var result = await AuthTokenIssuer.IssueAsync(user, roles, tokenService, dbContext, cancellationToken);

        await auditService.LogAsync("MfaVerified", nameof(User), user.Id.ToString(), "ok", user.Id, cancellationToken);
        return result;
    }
}
