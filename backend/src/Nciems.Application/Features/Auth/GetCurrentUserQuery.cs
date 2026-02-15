using MediatR;
using Microsoft.EntityFrameworkCore;
using Nciems.Application.Common.Exceptions;
using Nciems.Application.Interfaces;

namespace Nciems.Application.Features.Auth;

public sealed record CurrentUserNotificationDto(long Id, string Title, string Message, bool Read, DateTime CreatedAtUtc);

public sealed class CurrentUserDto
{
    public long Id { get; init; }
    public string UserName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public bool MfaEnabled { get; init; }
    public IReadOnlyCollection<string> Roles { get; init; } = [];
    public IReadOnlyCollection<CurrentUserNotificationDto> Notifications { get; init; } = [];
}

public sealed record GetCurrentUserQuery : IRequest<CurrentUserDto>;

public sealed class GetCurrentUserQueryHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext) : IRequestHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<CurrentUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            throw new ForbiddenException("Authentication is required.");
        }

        var user = await dbContext.Users
            .Include(x => x.UserRoles)
            .ThenInclude(x => x.Role)
            .SingleOrDefaultAsync(x => x.Id == userContext.UserId.Value, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("User not found.");
        }

        var notifications = await dbContext.Notifications
            .Where(x => x.UserId == user.Id)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(20)
            .Select(x => new CurrentUserNotificationDto(x.Id, x.Title, x.Message, x.Read, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return new CurrentUserDto
        {
            Id = user.Id,
            UserName = user.UserName,
            Email = user.Email,
            MfaEnabled = user.MfaEnabled,
            Roles = user.UserRoles.Select(x => x.Role!.Name).ToArray(),
            Notifications = notifications
        };
    }
}
