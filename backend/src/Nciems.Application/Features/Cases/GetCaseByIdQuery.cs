using MediatR;
using Microsoft.EntityFrameworkCore;
using Nciems.Application.Common.Exceptions;
using Nciems.Application.Interfaces;

namespace Nciems.Application.Features.Cases;

public sealed record GetCaseByIdQuery(long CaseId) : IRequest<CaseDetailsDto>;

public sealed class GetCaseByIdQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GetCaseByIdQuery, CaseDetailsDto>
{
    public async Task<CaseDetailsDto> Handle(GetCaseByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await dbContext.Cases
            .AsNoTracking()
            .Include(x => x.Complaint)
            .SingleOrDefaultAsync(x => x.Id == request.CaseId, cancellationToken);

        if (entity is null || entity.Complaint is null)
        {
            throw new NotFoundException("Case not found.");
        }

        return new CaseDetailsDto
        {
            Id = entity.Id,
            ComplaintId = entity.ComplaintId,
            ComplaintDescription = entity.Complaint.Description,
            ComplainantName = entity.Complaint.ComplainantName,
            Phone = entity.Complaint.Phone,
            Status = entity.Status,
            Priority = entity.Priority,
            AssignedInvestigatorId = entity.AssignedInvestigatorId,
            CreatedAtUtc = entity.CreatedAtUtc,
            ClosedAtUtc = entity.ClosedAtUtc
        };
    }
}
