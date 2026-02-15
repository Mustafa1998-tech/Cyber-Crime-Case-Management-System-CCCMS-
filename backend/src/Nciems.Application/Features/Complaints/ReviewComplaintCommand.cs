using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nciems.Application.Common.Exceptions;
using Nciems.Application.Interfaces;
using Nciems.Domain.Entities;
using Nciems.Domain.Enums;

namespace Nciems.Application.Features.Complaints;

public sealed record ReviewComplaintCommand(
    long ComplaintId,
    bool Approved,
    long? AssignedInvestigatorId,
    string? Priority,
    string? RejectionReason) : IRequest<long?>;

public sealed class ReviewComplaintCommandValidator : AbstractValidator<ReviewComplaintCommand>
{
    public ReviewComplaintCommandValidator()
    {
        RuleFor(x => x.ComplaintId).GreaterThan(0);
        RuleFor(x => x.Priority).MaximumLength(30);
        RuleFor(x => x.RejectionReason).MaximumLength(1000);
    }
}

public sealed class ReviewComplaintCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext,
    IAuditService auditService) : IRequestHandler<ReviewComplaintCommand, long?>
{
    public async Task<long?> Handle(ReviewComplaintCommand request, CancellationToken cancellationToken)
    {
        if (!userContext.IsInRole(RoleNames.SuperAdmin) && !userContext.IsInRole(RoleNames.SystemAdmin))
        {
            throw new ForbiddenException("Only administrators can review complaints.");
        }

        if (!userContext.UserId.HasValue)
        {
            throw new ForbiddenException("Authentication is required.");
        }

        var complaint = await dbContext.Complaints
            .Include(x => x.Case)
            .SingleOrDefaultAsync(x => x.Id == request.ComplaintId, cancellationToken);

        if (complaint is null)
        {
            throw new NotFoundException("Complaint not found.");
        }

        if (!request.Approved)
        {
            complaint.Status = ComplaintStatus.Rejected;
            await dbContext.SaveChangesAsync(cancellationToken);
            await auditService.LogAsync(
                "ComplaintRejected",
                nameof(Complaint),
                complaint.Id.ToString(),
                request.RejectionReason ?? string.Empty,
                userContext.UserId,
                cancellationToken);
            return null;
        }

        if (complaint.Case is not null)
        {
            throw new ConflictException("Complaint is already mapped to a case.");
        }

        complaint.Status = ComplaintStatus.Approved;

        if (request.AssignedInvestigatorId.HasValue)
        {
            var isInvestigator = await dbContext.UserRoles
                .Include(x => x.Role)
                .AnyAsync(
                    x => x.UserId == request.AssignedInvestigatorId.Value &&
                         x.Role!.Name == RoleNames.Investigator,
                    cancellationToken);

            if (!isInvestigator)
            {
                throw new ConflictException("Assigned user is not an investigator.");
            }
        }

        var newCase = new Case
        {
            ComplaintId = complaint.Id,
            Status = CaseStatus.New,
            Priority = string.IsNullOrWhiteSpace(request.Priority) ? "Medium" : request.Priority.Trim(),
            AssignedInvestigatorId = request.AssignedInvestigatorId
        };

        dbContext.Cases.Add(newCase);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (request.AssignedInvestigatorId.HasValue)
        {
            dbContext.CaseAssignments.Add(new CaseAssignment
            {
                CaseId = newCase.Id,
                InvestigatorId = request.AssignedInvestigatorId.Value,
                AssignedByUserId = userContext.UserId.Value
            });
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        await auditService.LogAsync(
            "ComplaintApprovedAndCaseCreated",
            nameof(Case),
            newCase.Id.ToString(),
            $"complaintId={complaint.Id}",
            userContext.UserId,
            cancellationToken);

        return newCase.Id;
    }
}
