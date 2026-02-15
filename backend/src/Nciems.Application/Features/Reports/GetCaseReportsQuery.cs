using MediatR;
using Microsoft.EntityFrameworkCore;
using Nciems.Application.Interfaces;

namespace Nciems.Application.Features.Reports;

public sealed record GetCaseReportsQuery(long CaseId) : IRequest<IReadOnlyCollection<ReportDto>>;

public sealed class GetCaseReportsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetCaseReportsQuery, IReadOnlyCollection<ReportDto>>
{
    public async Task<IReadOnlyCollection<ReportDto>> Handle(GetCaseReportsQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Reports
            .AsNoTracking()
            .Where(x => x.CaseId == request.CaseId)
            .OrderByDescending(x => x.GeneratedAtUtc)
            .Select(x => new ReportDto
            {
                Id = x.Id,
                CaseId = x.CaseId,
                ReportType = x.ReportType,
                Content = x.Content,
                GeneratedByUserId = x.GeneratedByUserId,
                GeneratedAtUtc = x.GeneratedAtUtc,
                DigitalSignature = x.DigitalSignature,
                QrPayload = x.QrPayload
            })
            .ToListAsync(cancellationToken);
    }
}
