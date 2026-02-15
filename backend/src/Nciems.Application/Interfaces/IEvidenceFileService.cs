using Nciems.Application.Common.Models;

namespace Nciems.Application.Interfaces;

public interface IEvidenceFileService
{
    Task<SavedEvidenceFile> SaveEncryptedAsync(
        byte[] fileBytes,
        string originalFileName,
        CancellationToken cancellationToken);

    Task<byte[]> ReadDecryptedAsync(
        string storedFilePath,
        string encryptionIv,
        CancellationToken cancellationToken);
}
