namespace Nciems.Application.Features.Evidence;

public sealed class EvidenceVersionMetaDto
{
    public long Id { get; init; }
    public int VersionNumber { get; init; }
    public string OriginalFileName { get; init; } = string.Empty;
    public string Sha256Hash { get; init; } = string.Empty;
    public string Md5Hash { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public string MimeType { get; init; } = string.Empty;
    public string DeviceInfo { get; init; } = string.Empty;
    public long UploadedByUserId { get; init; }
    public DateTime UploadedAtUtc { get; init; }
}

public sealed class EvidenceItemDto
{
    public long Id { get; init; }
    public long CaseId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime CreatedAtUtc { get; init; }
    public IReadOnlyCollection<EvidenceVersionMetaDto> Versions { get; init; } = [];
}

public sealed class EvidenceUploadResultDto
{
    public long EvidenceId { get; init; }
    public long EvidenceVersionId { get; init; }
    public int VersionNumber { get; init; }
    public string Sha256Hash { get; init; } = string.Empty;
    public string Md5Hash { get; init; } = string.Empty;
}

public sealed class EvidenceDownloadDto
{
    public string FileName { get; init; } = string.Empty;
    public string MimeType { get; init; } = "application/octet-stream";
    public byte[] FileBytes { get; init; } = [];
    public string Sha256Hash { get; init; } = string.Empty;
    public string Md5Hash { get; init; } = string.Empty;
}
