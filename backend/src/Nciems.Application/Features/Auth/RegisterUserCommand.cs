using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nciems.Application.Common.Exceptions;
using Nciems.Application.Common.Validation;
using Nciems.Application.Interfaces;
using Nciems.Domain.Entities;
using Nciems.Domain.Enums;

namespace Nciems.Application.Features.Auth;

public sealed record RegisterUserCommand(
    string UserName,
    string Email,
    string Password,
    bool MfaEnabled,
    IReadOnlyCollection<string>? Roles) : IRequest<long>;

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty()
            .MaximumLength(100)
            .Matches(@"^[a-zA-Z0-9._@\-]+$")
            .WithMessage("User name contains invalid characters.")
            .MustBeSafeText(nameof(RegisterUserCommand.UserName));

        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(12)
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches(@"\d").WithMessage("Password must contain at least one digit.")
            .Matches(@"[^\w\s]").WithMessage("Password must contain at least one special character.")
            .Must(password => !password.Any(char.IsWhiteSpace))
            .WithMessage("Password must not contain whitespace.");
    }
}

public sealed class RegisterUserCommandHandler(
    IApplicationDbContext dbContext,
    IPasswordHasher passwordHasher,
    IUserContext userContext,
    IAuditService auditService) : IRequestHandler<RegisterUserCommand, long>
{
    public async Task<long> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var hasAnyUser = await dbContext.Users.AnyAsync(cancellationToken);
        var isAdmin = userContext.IsInRole(RoleNames.SuperAdmin) || userContext.IsInRole(RoleNames.SystemAdmin);

        if (hasAnyUser && !isAdmin)
        {
            throw new ForbiddenException("Only system administrators can create users.");
        }

        if (await dbContext.Users.AnyAsync(x => x.UserName == request.UserName || x.Email == request.Email, cancellationToken))
        {
            throw new ConflictException("User name or email already exists.");
        }

        var requestedRoleNames = request.Roles is { Count: > 0 } ? request.Roles.Distinct().ToArray() : [RoleNames.IntakeOfficer];
        var roleEntities = await dbContext.Roles.Where(r => requestedRoleNames.Contains(r.Name)).ToListAsync(cancellationToken);

        if (roleEntities.Count != requestedRoleNames.Length)
        {
            throw new ConflictException("One or more roles are invalid.");
        }

        var password = passwordHasher.HashPassword(request.Password);
        var user = new User
        {
            UserName = request.UserName.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = password.Hash,
            PasswordSalt = password.Salt,
            MfaEnabled = request.MfaEnabled
        };

        foreach (var role in roleEntities)
        {
            user.UserRoles.Add(new UserRole { RoleId = role.Id, User = user });
        }

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "CreateUser",
            nameof(User),
            user.Id.ToString(),
            $"roles={string.Join(",", requestedRoleNames)}",
            userContext.UserId,
            cancellationToken);

        return user.Id;
    }
}
