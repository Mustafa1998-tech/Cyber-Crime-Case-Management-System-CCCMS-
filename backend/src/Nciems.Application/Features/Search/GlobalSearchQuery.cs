using System.Net;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Nciems.Application.Common.Validation;
using Nciems.Application.Interfaces;

namespace Nciems.Application.Features.Search;

public sealed class GlobalSearchResult
{
    public IReadOnlyCollection<long> CaseIds { get; init; } = [];
    public IReadOnlyCollection<long> EvidenceVersionIds { get; init; } = [];
    public IReadOnlyCollection<long> SuspectIds { get; init; } = [];
    public IReadOnlyCollection<long> ComplaintIds { get; init; } = [];
}

public sealed record GlobalSearchQuery(
    long? CaseId,
    string? Hash,
    string? Ip,
    string? Phone,
    string? SuspectName) : IRequest<GlobalSearchResult>;

public sealed class GlobalSearchQueryValidator : AbstractValidator<GlobalSearchQuery>
{
    public GlobalSearchQueryValidator()
    {
        RuleFor(x => x)
            .Must(x =>
                x.CaseId.HasValue ||
                !string.IsNullOrWhiteSpace(x.Hash) ||
                !string.IsNullOrWhiteSpace(x.Ip) ||
                !string.IsNullOrWhiteSpace(x.Phone) ||
                !string.IsNullOrWhiteSpace(x.SuspectName))
            .WithMessage("At least one search filter is required.");

        RuleFor(x => x.CaseId)
            .GreaterThan(0)
            .When(x => x.CaseId.HasValue);

        RuleFor(x => x.Hash)
            .MaximumLength(128)
            .Matches("^(?:[A-Fa-f0-9]{32}|[A-Fa-f0-9]{64})$")
            .When(x => !string.IsNullOrWhiteSpace(x.Hash))
            .WithMessage("Hash must be a valid MD5 or SHA-256 hex value.")
            .MustBeSafeOptionalText(nameof(GlobalSearchQuery.Hash), checkSqlPatterns: false);

        RuleFor(x => x.Ip)
            .MaximumLength(100)
            .Must(ip => string.IsNullOrWhiteSpace(ip) || IPAddress.TryParse(ip, out _))
            .WithMessage("IP address format is invalid.")
            .MustBeSafeOptionalText(nameof(GlobalSearchQuery.Ip));

        RuleFor(x => x.Phone)
            .MaximumLength(50)
            .Matches(@"^\+?[0-9()\-\s]{7,20}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Phone))
            .WithMessage("Phone format is invalid.")
            .MustBeSafeOptionalText(nameof(GlobalSearchQuery.Phone));

        RuleFor(x => x.SuspectName)
            .MaximumLength(200)
            .MustBeSafeOptionalText(nameof(GlobalSearchQuery.SuspectName));
    }
}

public sealed class GlobalSearchQueryHandler(IApplicationDbContext dbContext)
    : IRequestHandler<GlobalSearchQuery, GlobalSearchResult>
{
    public async Task<GlobalSearchResult> Handle(GlobalSearchQuery request, CancellationToken cancellationToken)
    {
        var caseIds = new HashSet<long>();
        var evidenceVersionIds = new HashSet<long>();
        var suspectIds = new HashSet<long>();
        var complaintIds = new HashSet<long>();

        if (request.CaseId.HasValue)
        {
            var exists = await dbContext.Cases.AnyAsync(x => x.Id == request.CaseId.Value, cancellationToken);
            if (exists)
            {
                caseIds.Add(request.CaseId.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Hash))
        {
            var hash = request.Hash.Trim().ToLowerInvariant();
            var matches = await dbContext.EvidenceVersions
                .Where(x => x.Sha256Hash.ToLower() == hash || x.Md5Hash.ToLower() == hash)
                .Select(x => new { x.Id, x.Evidence!.CaseId })
                .ToListAsync(cancellationToken);

            foreach (var item in matches)
            {
                evidenceVersionIds.Add(item.Id);
                caseIds.Add(item.CaseId);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Ip))
        {
            var ip = request.Ip.Trim();
            var suspectsByIp = await dbContext.Suspects
                .Where(x => x.IpAddress == ip)
                .Select(x => new { x.Id, x.CaseId })
                .ToListAsync(cancellationToken);

            foreach (var item in suspectsByIp)
            {
                suspectIds.Add(item.Id);
                caseIds.Add(item.CaseId);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Phone))
        {
            var phone = request.Phone.Trim();

            var complaintsByPhone = await dbContext.Complaints
                .Where(x => x.Phone == phone)
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            foreach (var id in complaintsByPhone)
            {
                complaintIds.Add(id);
            }

            var suspectsByPhone = await dbContext.Suspects
                .Where(x => x.Phone == phone)
                .Select(x => new { x.Id, x.CaseId })
                .ToListAsync(cancellationToken);

            foreach (var item in suspectsByPhone)
            {
                suspectIds.Add(item.Id);
                caseIds.Add(item.CaseId);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.SuspectName))
        {
            var name = request.SuspectName.Trim();
            var suspectsByName = await dbContext.Suspects
                .Where(x => x.Name.Contains(name))
                .Select(x => new { x.Id, x.CaseId })
                .ToListAsync(cancellationToken);

            foreach (var item in suspectsByName)
            {
                suspectIds.Add(item.Id);
                caseIds.Add(item.CaseId);
            }
        }

        return new GlobalSearchResult
        {
            CaseIds = caseIds.ToArray(),
            EvidenceVersionIds = evidenceVersionIds.ToArray(),
            SuspectIds = suspectIds.ToArray(),
            ComplaintIds = complaintIds.ToArray()
        };
    }
}
