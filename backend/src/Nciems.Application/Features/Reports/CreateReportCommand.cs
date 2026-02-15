using System.Security.Cryptography;
using System.Text;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nciems.Application.Common.Exceptions;
using Nciems.Application.Common.Validation;
using Nciems.Application.Interfaces;
using Nciems.Domain.Entities;
using Nciems.Domain.Enums;

namespace Nciems.Application.Features.Reports;

public sealed record CreateReportCommand(long CaseId, ReportType ReportType, string Content) : IRequest<long>;

public sealed class CreateReportCommandValidator : AbstractValidator<CreateReportCommand>
{
    public CreateReportCommandValidator()
    {
        RuleFor(x => x.CaseId).GreaterThan(0);
        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(10000)
            .MustBeSafeText(nameof(CreateReportCommand.Content), checkSqlPatterns: false);
    }
}

public sealed class CreateReportCommandHandler(
    IApplicationDbContext dbContext,
    IUserContext userContext,
    IAuditService auditService) : IRequestHandler<CreateReportCommand, long>
{
    public async Task<long> Handle(CreateReportCommand request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            throw new ForbiddenException("Authentication is required.");
        }

        var canCreate = userContext.IsInRole(RoleNames.ForensicAnalyst) ||
                        userContext.IsInRole(RoleNames.SystemAdmin) ||
                        userContext.IsInRole(RoleNames.SuperAdmin);

        if (!canCreate)
        {
            throw new ForbiddenException("You are not allowed to create reports.");
        }

        var caseExists = await dbContext.Cases.AnyAsync(x => x.Id == request.CaseId, cancellationToken);
        if (!caseExists)
        {
            throw new NotFoundException("Case not found.");
        }

        var signaturePayload = $"{request.CaseId}|{request.ReportType}|{request.Content}|{DateTime.UtcNow:O}";
        var signature = ComputeSha256(signaturePayload);

        var report = new Report
        {
            CaseId = request.CaseId,
            ReportType = request.ReportType,
            Content = request.Content.Trim(),
            GeneratedByUserId = userContext.UserId.Value,
            DigitalSignature = signature,
            QrPayload = $"case:{request.CaseId};sig:{signature}"
        };

        dbContext.Reports.Add(report);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "ReportCreated",
            nameof(Report),
            report.Id.ToString(),
            report.ReportType.ToString(),
            userContext.UserId,
            cancellationToken);

        return report.Id;
    }

    private static string ComputeSha256(string payload)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
