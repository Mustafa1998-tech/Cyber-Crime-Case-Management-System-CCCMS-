using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nciems.Application.Common.Exceptions;
using Nciems.Application.Common.Validation;
using Nciems.Application.Interfaces;
using Nciems.Domain.Entities;
using Nciems.Domain.Enums;

namespace Nciems.Application.Features.Suspects;

public sealed record AddSuspectCommand(
    long CaseId,
    string Name,
    string? NationalId,
    string? Phone,
    string? IpAddress,
    string? AccountInfo,
    string? Notes) : IRequest<long>;

public sealed class AddSuspectCommandValidator : AbstractValidator<AddSuspectCommand>
{
    public AddSuspectCommandValidator()
    {
        RuleFor(x => x.CaseId).GreaterThan(0);
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200)
            .MustBeSafeText(nameof(AddSuspectCommand.Name));

        RuleFor(x => x.NationalId)
            .MaximumLength(50)
            .MustBeSafeOptionalText(nameof(AddSuspectCommand.NationalId), checkSqlPatterns: false);

        RuleFor(x => x.Phone)
            .MaximumLength(50)
            .Matches(@"^\+?[0-9()\-\s]{7,20}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("Phone format is invalid.");

        RuleFor(x => x.IpAddress)
            .MaximumLength(100)
            .Matches(@"^(([0-9]{1,3}\.){3}[0-9]{1,3}|[A-Fa-f0-9:]+)$")
            .When(x => !string.IsNullOrWhiteSpace(x.IpAddress))
            .WithMessage("IP address format is invalid.");

        RuleFor(x => x.AccountInfo)
            .MaximumLength(250)
            .MustBeSafeOptionalText(nameof(AddSuspectCommand.AccountInfo));

        RuleFor(x => x.Notes)
            .MaximumLength(2000)
            .MustBeSafeOptionalText(nameof(AddSuspectCommand.Notes), checkSqlPatterns: false);
    }
}

public sealed class AddSuspectCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext,
    IAuditService auditService) : IRequestHandler<AddSuspectCommand, long>
{
    public async Task<long> Handle(AddSuspectCommand request, CancellationToken cancellationToken)
    {
        if (!userContext.IsInRole(RoleNames.Investigator) &&
            !userContext.IsInRole(RoleNames.SystemAdmin) &&
            !userContext.IsInRole(RoleNames.SuperAdmin))
        {
            throw new ForbiddenException("Only investigators and admins can add suspects.");
        }

        var exists = await dbContext.Cases.AnyAsync(x => x.Id == request.CaseId, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException("Case not found.");
        }

        var suspect = new Suspect
        {
            CaseId = request.CaseId,
            Name = request.Name.Trim(),
            NationalId = request.NationalId?.Trim(),
            Phone = request.Phone?.Trim(),
            IpAddress = request.IpAddress?.Trim(),
            AccountInfo = request.AccountInfo?.Trim(),
            Notes = request.Notes?.Trim()
        };

        dbContext.Suspects.Add(suspect);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "SuspectAdded",
            nameof(Suspect),
            suspect.Id.ToString(),
            $"caseId={request.CaseId}",
            userContext.UserId,
            cancellationToken);

        return suspect.Id;
    }
}
