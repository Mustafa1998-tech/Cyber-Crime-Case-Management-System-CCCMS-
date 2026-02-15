using Nciems.Domain.Common;

namespace Nciems.Domain.Entities;

public sealed class EvidenceVersion : BaseEntity
{
    public long EvidenceId { get; set; }
    public int VersionNumber { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFilePath { get; set; } = string.Empty;
    public string Sha256Hash { get; set; } = string.Empty;
    public string Md5Hash { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string EncryptionIv { get; set; } = string.Empty;
    public string DeviceInfo { get; set; } = string.Empty;
    public long UploadedByUserId { get; set; }
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;

    public Evidence? Evidence { get; set; }
    public User? UploadedByUser { get; set; }
    public ICollection<EvidenceAccessLog> AccessLogs { get; set; } = new List<EvidenceAccessLog>();
}
