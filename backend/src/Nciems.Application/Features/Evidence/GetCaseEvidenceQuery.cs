using MediatR;
using Microsoft.EntityFrameworkCore;
using Nciems.Application.Interfaces;

namespace Nciems.Application.Features.Evidence;

public sealed record GetCaseEvidenceQuery(long CaseId) : IRequest<IReadOnlyCollection<EvidenceItemDto>>;

public sealed class GetCaseEvidenceQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetCaseEvidenceQuery, IReadOnlyCollection<EvidenceItemDto>>
{
    public async Task<IReadOnlyCollection<EvidenceItemDto>> Handle(GetCaseEvidenceQuery request, CancellationToken cancellationToken)
    {
        var data = await dbContext.Evidence
            .AsNoTracking()
            .Where(x => x.CaseId == request.CaseId)
            .Include(x => x.Versions)
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return data.Select(x => new EvidenceItemDto
        {
            Id = x.Id,
            CaseId = x.CaseId,
            Title = x.Title,
            Description = x.Description,
            CreatedAtUtc = x.CreatedAtUtc,
            Versions = x.Versions
                .OrderByDescending(v => v.VersionNumber)
                .Select(v => new EvidenceVersionMetaDto
                {
                    Id = v.Id,
                    VersionNumber = v.VersionNumber,
                    OriginalFileName = v.OriginalFileName,
                    Sha256Hash = v.Sha256Hash,
                    Md5Hash = v.Md5Hash,
                    FileSizeBytes = v.FileSizeBytes,
                    MimeType = v.MimeType,
                    DeviceInfo = v.DeviceInfo,
                    UploadedByUserId = v.UploadedByUserId,
                    UploadedAtUtc = v.UploadedAtUtc
                })
                .ToArray()
        }).ToArray();
    }
}
