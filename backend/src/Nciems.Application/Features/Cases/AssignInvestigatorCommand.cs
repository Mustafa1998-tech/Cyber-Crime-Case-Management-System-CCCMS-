using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nciems.Application.Common.Exceptions;
using Nciems.Application.Interfaces;
using Nciems.Domain.Entities;
using Nciems.Domain.Enums;

namespace Nciems.Application.Features.Cases;

public sealed record AssignInvestigatorCommand(long CaseId, long InvestigatorId) : IRequest;

public sealed class AssignInvestigatorCommandValidator : AbstractValidator<AssignInvestigatorCommand>
{
    public AssignInvestigatorCommandValidator()
    {
        RuleFor(x => x.CaseId).GreaterThan(0);
        RuleFor(x => x.InvestigatorId).GreaterThan(0);
    }
}

public sealed class AssignInvestigatorCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext,
    IAuditService auditService) : IRequestHandler<AssignInvestigatorCommand>
{
    public async Task Handle(AssignInvestigatorCommand request, CancellationToken cancellationToken)
    {
        if (!userContext.IsInRole(RoleNames.SuperAdmin) && !userContext.IsInRole(RoleNames.SystemAdmin))
        {
            throw new ForbiddenException("Only administrators can assign investigators.");
        }

        if (!userContext.UserId.HasValue)
        {
            throw new ForbiddenException("Authentication is required.");
        }

        var caseEntity = await dbContext.Cases.SingleOrDefaultAsync(x => x.Id == request.CaseId, cancellationToken);
        if (caseEntity is null)
        {
            throw new NotFoundException("Case not found.");
        }

        var isInvestigator = await dbContext.UserRoles
            .Include(x => x.Role)
            .AnyAsync(x => x.UserId == request.InvestigatorId && x.Role!.Name == RoleNames.Investigator, cancellationToken);

        if (!isInvestigator)
        {
            throw new ConflictException("Target user is not an investigator.");
        }

        caseEntity.AssignedInvestigatorId = request.InvestigatorId;
        dbContext.CaseAssignments.Add(new CaseAssignment
        {
            CaseId = caseEntity.Id,
            InvestigatorId = request.InvestigatorId,
            AssignedByUserId = userContext.UserId.Value
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await auditService.LogAsync(
            "CaseAssigned",
            nameof(Case),
            caseEntity.Id.ToString(),
            $"investigatorId={request.InvestigatorId}",
            userContext.UserId,
            cancellationToken);
    }
}
