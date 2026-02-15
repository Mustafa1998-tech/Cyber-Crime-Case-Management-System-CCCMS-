using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nciems.Application.Common.Exceptions;
using Nciems.Application.Interfaces;
using Nciems.Domain.Entities;
using Nciems.Domain.Enums;

namespace Nciems.Application.Features.Cases;

public sealed record ChangeCaseStatusCommand(long CaseId, CaseStatus NewStatus) : IRequest;

public sealed class ChangeCaseStatusCommandValidator : AbstractValidator<ChangeCaseStatusCommand>
{
    public ChangeCaseStatusCommandValidator()
    {
        RuleFor(x => x.CaseId).GreaterThan(0);
    }
}

public sealed class ChangeCaseStatusCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext,
    IAuditService auditService) : IRequestHandler<ChangeCaseStatusCommand>
{
    public async Task Handle(ChangeCaseStatusCommand request, CancellationToken cancellationToken)
    {
        if (!userContext.IsInRole(RoleNames.Investigator) &&
            !userContext.IsInRole(RoleNames.SystemAdmin) &&
            !userContext.IsInRole(RoleNames.SuperAdmin))
        {
            throw new ForbiddenException("You are not allowed to change case status.");
        }

        var caseEntity = await dbContext.Cases.SingleOrDefaultAsync(x => x.Id == request.CaseId, cancellationToken);
        if (caseEntity is null)
        {
            throw new NotFoundException("Case not found.");
        }

        if (userContext.IsInRole(RoleNames.Investigator) &&
            caseEntity.AssignedInvestigatorId.HasValue &&
            caseEntity.AssignedInvestigatorId != userContext.UserId)
        {
            throw new ForbiddenException("Only the assigned investigator can update this case.");
        }

        var allowed = IsTransitionAllowed(caseEntity.Status, request.NewStatus);
        if (!allowed)
        {
            throw new ConflictException($"Invalid status transition from {caseEntity.Status} to {request.NewStatus}.");
        }

        if (request.NewStatus == CaseStatus.Closed)
        {
            var hasAnalystReport = await dbContext.Reports.AnyAsync(
                x => x.CaseId == caseEntity.Id && x.ReportType == ReportType.AnalystTechnical,
                cancellationToken);

            if (!hasAnalystReport)
            {
                throw new ConflictException("Case cannot be closed without analyst technical report.");
            }

            caseEntity.ClosedAtUtc = DateTime.UtcNow;
        }

        var oldStatus = caseEntity.Status;
        caseEntity.Status = request.NewStatus;
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "CaseStatusChanged",
            nameof(Case),
            caseEntity.Id.ToString(),
            $"{oldStatus}=>{request.NewStatus}",
            userContext.UserId,
            cancellationToken);
    }

    private static bool IsTransitionAllowed(CaseStatus current, CaseStatus next)
    {
        if (current == next)
        {
            return true;
        }

        return (current, next) switch
        {
            (CaseStatus.New, CaseStatus.UnderInvestigation) => true,
            (CaseStatus.UnderInvestigation, CaseStatus.ForensicAnalysis) => true,
            (CaseStatus.ForensicAnalysis, CaseStatus.ProsecutorReview) => true,
            (CaseStatus.ProsecutorReview, CaseStatus.Closed) => true,
            _ => false
        };
    }
}
