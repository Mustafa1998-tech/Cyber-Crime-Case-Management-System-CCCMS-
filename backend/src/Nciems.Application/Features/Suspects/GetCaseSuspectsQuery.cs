using MediatR;
using Microsoft.EntityFrameworkCore;
using Nciems.Application.Interfaces;

namespace Nciems.Application.Features.Suspects;

public sealed record GetCaseSuspectsQuery(long CaseId) : IRequest<IReadOnlyCollection<SuspectDto>>;

public sealed class GetCaseSuspectsQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetCaseSuspectsQuery, IReadOnlyCollection<SuspectDto>>
{
    public async Task<IReadOnlyCollection<SuspectDto>> Handle(GetCaseSuspectsQuery request, CancellationToken cancellationToken)
    {
        return await dbContext.Suspects
            .AsNoTracking()
            .Where(x => x.CaseId == request.CaseId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new SuspectDto
            {
                Id = x.Id,
                CaseId = x.CaseId,
                Name = x.Name,
                NationalId = x.NationalId,
                Phone = x.Phone,
                IpAddress = x.IpAddress,
                AccountInfo = x.AccountInfo,
                Notes = x.Notes,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }
}
