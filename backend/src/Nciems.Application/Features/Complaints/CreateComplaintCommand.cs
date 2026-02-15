using FluentValidation;
using MediatR;
using Nciems.Application.Common.Exceptions;
using Nciems.Application.Common.Validation;
using Nciems.Application.Interfaces;
using Nciems.Domain.Entities;
using Nciems.Domain.Enums;

namespace Nciems.Application.Features.Complaints;

public sealed record CreateComplaintCommand(
    string ComplainantName,
    string Phone,
    string CrimeType,
    string Description) : IRequest<long>;

public sealed class CreateComplaintCommandValidator : AbstractValidator<CreateComplaintCommand>
{
    public CreateComplaintCommandValidator()
    {
        RuleFor(x => x.ComplainantName)
            .NotEmpty()
            .MaximumLength(200)
            .MustBeSafeText(nameof(CreateComplaintCommand.ComplainantName));

        RuleFor(x => x.Phone)
            .NotEmpty()
            .MaximumLength(50)
            .Matches(@"^\+?[0-9()\-\s]{7,20}$")
            .WithMessage("Phone format is invalid.");

        RuleFor(x => x.CrimeType)
            .NotEmpty()
            .MaximumLength(100)
            .MustBeSafeText(nameof(CreateComplaintCommand.CrimeType), checkSqlPatterns: false);

        RuleFor(x => x.Description)
            .NotEmpty()
            .MaximumLength(4000)
            .MustBeSafeText(nameof(CreateComplaintCommand.Description), checkSqlPatterns: false);
    }
}

public sealed class CreateComplaintCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext,
    IAuditService auditService) : IRequestHandler<CreateComplaintCommand, long>
{
    public async Task<long> Handle(CreateComplaintCommand request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            throw new ForbiddenException("Authentication is required.");
        }

        var canCreate = userContext.IsInRole(RoleNames.IntakeOfficer) ||
                        userContext.IsInRole(RoleNames.SystemAdmin) ||
                        userContext.IsInRole(RoleNames.SuperAdmin);

        if (!canCreate)
        {
            throw new ForbiddenException("You are not allowed to create complaints.");
        }

        var complaint = new Complaint
        {
            ComplainantName = request.ComplainantName.Trim(),
            Phone = request.Phone.Trim(),
            CrimeType = request.CrimeType.Trim(),
            Description = request.Description.Trim(),
            Status = ComplaintStatus.New,
            CreatedByUserId = userContext.UserId.Value
        };

        dbContext.Complaints.Add(complaint);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "ComplaintCreated",
            nameof(Complaint),
            complaint.Id.ToString(),
            complaint.CrimeType,
            userContext.UserId,
            cancellationToken);

        return complaint.Id;
    }
}
