using MediatR;
using Microsoft.EntityFrameworkCore;
using Nciems.Application.Common.Exceptions;
using Nciems.Application.Interfaces;
using Nciems.Domain.Entities;
using Nciems.Domain.Enums;

namespace Nciems.Application.Features.Evidence;

public sealed record DownloadEvidenceVersionQuery(long EvidenceVersionId, EvidenceAccessType AccessType)
    : IRequest<EvidenceDownloadDto>;

public sealed class DownloadEvidenceVersionQueryHandler(
    IApplicationDbContext dbContext,
    IEvidenceFileService evidenceFileService,
    IUserContext userContext,
    IAuditService auditService) : IRequestHandler<DownloadEvidenceVersionQuery, EvidenceDownloadDto>
{
    public async Task<EvidenceDownloadDto> Handle(DownloadEvidenceVersionQuery request, CancellationToken cancellationToken)
    {
        if (!userContext.UserId.HasValue)
        {
            throw new ForbiddenException("Authentication is required.");
        }

        var canRead = userContext.IsInRole(RoleNames.Investigator) ||
                      userContext.IsInRole(RoleNames.ForensicAnalyst) ||
                      userContext.IsInRole(RoleNames.Prosecutor) ||
                      userContext.IsInRole(RoleNames.SystemAdmin) ||
                      userContext.IsInRole(RoleNames.SuperAdmin);

        if (!canRead)
        {
            throw new ForbiddenException("You are not allowed to view evidence.");
        }

        var version = await dbContext.EvidenceVersions
            .Include(x => x.Evidence)
            .SingleOrDefaultAsync(x => x.Id == request.EvidenceVersionId, cancellationToken);

        if (version is null)
        {
            throw new NotFoundException("Evidence version not found.");
        }

        var bytes = await evidenceFileService.ReadDecryptedAsync(version.StoredFilePath, version.EncryptionIv, cancellationToken);
        dbContext.EvidenceAccessLogs.Add(new EvidenceAccessLog
        {
            EvidenceVersionId = version.Id,
            AccessedByUserId = userContext.UserId.Value,
            AccessType = request.AccessType
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditService.LogAsync(
            "EvidenceAccessed",
            nameof(EvidenceVersion),
            version.Id.ToString(),
            request.AccessType.ToString(),
            userContext.UserId,
            cancellationToken);

        return new EvidenceDownloadDto
        {
            FileName = version.OriginalFileName,
            MimeType = version.MimeType,
            FileBytes = bytes,
            Sha256Hash = version.Sha256Hash,
            Md5Hash = version.Md5Hash
        };
    }
}
