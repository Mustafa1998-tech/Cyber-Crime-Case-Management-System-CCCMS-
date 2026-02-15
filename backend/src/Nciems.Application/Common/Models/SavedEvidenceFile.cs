namespace Nciems.Application.Common.Models;

public sealed record SavedEvidenceFile(
    string StoredFilePath,
    string Sha256Hash,
    string Md5Hash,
    long FileSizeBytes,
    string EncryptionIv);
