using MediatR;
using Microsoft.EntityFrameworkCore;
using Nciems.Application.Interfaces;
using Nciems.Domain.Enums;

namespace Nciems.Application.Features.Complaints;

public sealed record GetComplaintsQuery(ComplaintStatus? Status) : IRequest<IReadOnlyCollection<ComplaintDto>>;

public sealed class GetComplaintsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetComplaintsQuery, IReadOnlyCollection<ComplaintDto>>
{
    public async Task<IReadOnlyCollection<ComplaintDto>> Handle(GetComplaintsQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Complaints
            .AsNoTracking()
            .Include(x => x.Case)
            .AsQueryable();

        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }

        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new ComplaintDto
            {
                Id = x.Id,
                ComplainantName = x.ComplainantName,
                Phone = x.Phone,
                CrimeType = x.CrimeType,
                Description = x.Description,
                Status = x.Status,
                CreatedByUserId = x.CreatedByUserId,
                CreatedAtUtc = x.CreatedAtUtc,
                CaseId = x.Case != null ? x.Case.Id : null
            })
            .ToListAsync(cancellationToken);
    }
}
