using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nciems.Application.Common.Exceptions;
using Nciems.Application.Common.Models;
using Nciems.Application.Common.Validation;
using Nciems.Application.Interfaces;
using Nciems.Domain.Entities;
using Nciems.Domain.Enums;

namespace Nciems.Application.Features.Auth;

public sealed record LoginCommand(string UserName, string Password, string DeviceInfo) : IRequest<AuthResult>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty()
            .MaximumLength(100)
            .MustBeSafeText(nameof(LoginCommand.UserName));

        RuleFor(x => x.Password).NotEmpty();

        RuleFor(x => x.DeviceInfo)
            .NotEmpty()
            .MaximumLength(200)
            .MustBeSafeText(nameof(LoginCommand.DeviceInfo), checkSqlPatterns: false);
    }
}

public sealed class LoginCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IAuditService auditService) : IRequestHandler<LoginCommand, AuthResult>
{
    public async Task<AuthResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var identity = request.UserName.Trim();
        var identityLower = identity.ToLowerInvariant();

        var user = await dbContext.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(
                x => x.UserName.ToLower() == identityLower || x.Email.ToLower() == identityLower,
                cancellationToken);

        if (user is null)
        {
            await auditService.LogAsync("LoginFailed", nameof(User), "unknown", "invalid_user", null, cancellationToken);
            throw new ForbiddenException("Invalid credentials.");
        }

        if (user.IsLocked && user.LockoutEndUtc.HasValue && user.LockoutEndUtc > DateTime.UtcNow)
        {
            await auditService.LogAsync("LoginBlocked", nameof(User), user.Id.ToString(), "user_locked", user.Id, cancellationToken);
            throw new ForbiddenException("User is locked due to repeated failed logins.");
        }

        if (!passwordHasher.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            user.FailedLoginAttempts += 1;
            if (user.FailedLoginAttempts >= 5)
            {
                user.IsLocked = true;
                user.LockoutEndUtc = DateTime.UtcNow.AddMinutes(30);
            }

            await dbContext.SaveChangesAsync(cancellationToken);
            await auditService.LogAsync("LoginFailed", nameof(User), user.Id.ToString(), "invalid_password", user.Id, cancellationToken);
            throw new ForbiddenException("Invalid credentials.");
        }

        user.FailedLoginAttempts = 0;
        user.IsLocked = false;
        user.LockoutEndUtc = null;
        user.UpdatedAtUtc = DateTime.UtcNow;

        var roles = user.UserRoles.Select(x => x.Role!.Name).ToArray();
        var isPrivileged = roles.Any(r => RoleNames.PrivilegedRoles.Contains(r));

        if (isPrivileged && user.MfaEnabled)
        {
            var otpCode = tokenService.GenerateOtpCode();
            user.PendingMfaCodeHash = tokenService.HashValue(otpCode);
            user.PendingMfaCodeExpiresAtUtc = DateTime.UtcNow.AddMinutes(5);

            dbContext.Notifications.Add(new Notification
            {
                UserId = user.Id,
                Title = "MFA Code",
                Message = $"Your OTP code is: {otpCode}"
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            await auditService.LogAsync("MfaChallengeCreated", nameof(User), user.Id.ToString(), request.DeviceInfo, user.Id, cancellationToken);

            return new AuthResult
            {
                MfaRequired = true,
                Message = "MFA verification is required. Check user notifications for OTP.",
                UserName = user.UserName,
                Roles = roles
            };
        }

        var result = await AuthTokenIssuer.IssueAsync(user, roles, tokenService, dbContext, cancellationToken);
        await auditService.LogAsync("LoginSuccess", nameof(User), user.Id.ToString(), request.DeviceInfo, user.Id, cancellationToken);
        return result;
    }
}
