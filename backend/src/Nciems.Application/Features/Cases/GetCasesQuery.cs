using MediatR;
using Microsoft.EntityFrameworkCore;
using Nciems.Application.Interfaces;
using Nciems.Domain.Enums;

namespace Nciems.Application.Features.Cases;

public sealed record GetCasesQuery(CaseStatus? Status) : IRequest<IReadOnlyCollection<CaseListItemDto>>;

public sealed class GetCasesQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetCasesQuery, IReadOnlyCollection<CaseListItemDto>>
{
    public async Task<IReadOnlyCollection<CaseListItemDto>> Handle(GetCasesQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Cases
            .AsNoTracking()
            .Include(x => x.Complaint)
            .AsQueryable();

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new CaseListItemDto
            {
                Id = x.Id,
                ComplaintId = x.ComplaintId,
                ComplaintCrimeType = x.Complaint!.CrimeType,
                ComplaintPhone = x.Complaint.Phone,
                Status = x.Status,
                Priority = x.Priority,
                AssignedInvestigatorId = x.AssignedInvestigatorId,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }
}
