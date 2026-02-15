using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nciems.Application.Common.Exceptions;
using Nciems.Application.Common.Validation;
using Nciems.Application.Interfaces;
using Nciems.Domain.Entities;
using Nciems.Domain.Enums;

namespace Nciems.Application.Features.Evidence;

public sealed record UploadEvidenceCommand(
    long CaseId,
    long? ExistingEvidenceId,
    string Title,
    string? Description,
    string FileName,
    string MimeType,
    string DeviceInfo,
    byte[] FileBytes) : IRequest<EvidenceUploadResultDto>;

public sealed class UploadEvidenceCommandValidator : AbstractValidator<UploadEvidenceCommand>
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".png", ".jpg", ".jpeg", ".txt", ".csv", ".json", ".zip", ".7z", ".mp3", ".wav", ".mp4"
    };

    public UploadEvidenceCommandValidator()
    {
        RuleFor(x => x.CaseId).GreaterThan(0);
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200)
            .MustBeSafeText(nameof(UploadEvidenceCommand.Title), checkSqlPatterns: false);

        RuleFor(x => x.Description)
            .MaximumLength(2000)
            .MustBeSafeOptionalText(nameof(UploadEvidenceCommand.Description), checkSqlPatterns: false);

        RuleFor(x => x.FileName)
            .NotEmpty()
            .MaximumLength(260)
            .Must(name => Path.GetFileName(name) == name)
            .WithMessage("File name contains invalid path characters.")
            .Must(name => AllowedExtensions.Contains(Path.GetExtension(name)))
            .WithMessage("File extension is not allowed.");

        RuleFor(x => x.MimeType).NotEmpty().MaximumLength(200);

        RuleFor(x => x.DeviceInfo)
            .NotEmpty()
            .MaximumLength(200)
            .MustBeSafeText(nameof(UploadEvidenceCommand.DeviceInfo), checkSqlPatterns: false);

        RuleFor(x => x.FileBytes)
            .NotEmpty()
            .Must(bytes => bytes.LongLength <= 50_000_000)
            .WithMessage("File exceeds maximum allowed size (50 MB).");
    }
}

public sealed class UploadEvidenceCommandHandler(
    IApplicationDbContext dbContext,
    IEvidenceFileService evidenceFileService,
    IUserContext userContext,
    IAuditService auditService) : IRequestHandler<UploadEvidenceCommand, EvidenceUploadResultDto>
{
    public async Task<EvidenceUploadResultDto> Handle(UploadEvidenceCommand request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            throw new ForbiddenException("Authentication is required.");
        }

        var canUpload = userContext.IsInRole(RoleNames.Investigator) ||
                        userContext.IsInRole(RoleNames.ForensicAnalyst) ||
                        userContext.IsInRole(RoleNames.SystemAdmin) ||
                        userContext.IsInRole(RoleNames.SuperAdmin);

        if (!canUpload)
        {
            throw new ForbiddenException("You are not allowed to upload evidence.");
        }

        var caseExists = await dbContext.Cases.AnyAsync(x => x.Id == request.CaseId, cancellationToken);
        if (!caseExists)
        {
            throw new NotFoundException("Case not found.");
        }

        Nciems.Domain.Entities.Evidence evidence;
        if (request.ExistingEvidenceId.HasValue)
        {
            evidence = await dbContext.Evidence
                .Include(x => x.Versions)
                .SingleOrDefaultAsync(
                    x => x.Id == request.ExistingEvidenceId.Value && x.CaseId == request.CaseId,
                    cancellationToken)
                ?? throw new NotFoundException("Evidence item not found.");
        }
        else
        {
            evidence = new Nciems.Domain.Entities.Evidence
            {
                CaseId = request.CaseId,
                Title = request.Title.Trim(),
                Description = request.Description?.Trim(),
                CreatedByUserId = userContext.UserId.Value
            };

            dbContext.Evidence.Add(evidence);
        }

        var savedFile = await evidenceFileService.SaveEncryptedAsync(request.FileBytes, request.FileName, cancellationToken);
        var nextVersion = evidence.Versions.Any() ? evidence.Versions.Max(x => x.VersionNumber) + 1 : 1;

        var version = new EvidenceVersion
        {
            Evidence = evidence,
            VersionNumber = nextVersion,
            OriginalFileName = request.FileName.Trim(),
            StoredFilePath = savedFile.StoredFilePath,
            Sha256Hash = savedFile.Sha256Hash,
            Md5Hash = savedFile.Md5Hash,
            FileSizeBytes = savedFile.FileSizeBytes,
            MimeType = request.MimeType.Trim(),
            EncryptionIv = savedFile.EncryptionIv,
            DeviceInfo = request.DeviceInfo.Trim(),
            UploadedByUserId = userContext.UserId.Value
        };

        dbContext.EvidenceVersions.Add(version);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "EvidenceVersionUploaded",
            nameof(EvidenceVersion),
            version.Id.ToString(),
            $"caseId={request.CaseId};evidenceId={evidence.Id};version={nextVersion}",
            userContext.UserId,
            cancellationToken);

        return new EvidenceUploadResultDto
        {
            EvidenceId = evidence.Id,
            EvidenceVersionId = version.Id,
            VersionNumber = version.VersionNumber,
            Sha256Hash = version.Sha256Hash,
            Md5Hash = version.Md5Hash
        };
    }
}
