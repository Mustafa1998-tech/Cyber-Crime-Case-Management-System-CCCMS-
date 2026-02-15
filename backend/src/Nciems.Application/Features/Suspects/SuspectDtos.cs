namespace Nciems.Application.Features.Suspects;

public sealed class SuspectDto
{
    public long Id { get; init; }
    public long CaseId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? NationalId { get; init; }
    public string? Phone { get; init; }
    public string? IpAddress { get; init; }
    public string? AccountInfo { get; init; }
    public string? Notes { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
