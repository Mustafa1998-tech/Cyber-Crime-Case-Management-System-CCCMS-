namespace Nciems.Infrastructure.Options;

public sealed class EvidenceStorageOptions
{
    public const string SectionName = "EvidenceStorage";

    public string RootPath { get; set; } = "EvidenceStore";

    // Must be base64-encoded 32-byte key for AES-256.
    public string EncryptionKey { get; set; } = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";
}
