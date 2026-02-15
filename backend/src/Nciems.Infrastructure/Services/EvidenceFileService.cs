using System.Security.Cryptography;
using Nciems.Application.Common.Models;
using Nciems.Application.Interfaces;
using Nciems.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Nciems.Infrastructure.Services;

public sealed class EvidenceFileService(IOptions<EvidenceStorageOptions> options) : IEvidenceFileService
{
    private readonly EvidenceStorageOptions storageOptions = options.Value;

    public async Task<SavedEvidenceFile> SaveEncryptedAsync(
        byte[] fileBytes,
        string originalFileName,
        CancellationToken cancellationToken)
    {
        var root = Path.GetFullPath(storageOptions.RootPath);
        var year = DateTime.UtcNow.ToString("yyyy");
        var month = DateTime.UtcNow.ToString("MM");
        var day = DateTime.UtcNow.ToString("dd");
        var folder = Path.Combine(root, year, month, day);
        Directory.CreateDirectory(folder);

        var fileName = $"{Guid.NewGuid():N}.bin";
        var filePath = Path.Combine(folder, fileName);

        var key = Convert.FromBase64String(storageOptions.EncryptionKey);
        if (key.Length != 32)
        {
            throw new InvalidOperationException("Evidence encryption key must be exactly 32 bytes (base64).");
        }

        using var sha = SHA256.Create();
        using var md5 = MD5.Create();
        var sha256Hash = Convert.ToHexString(sha.ComputeHash(fileBytes)).ToLowerInvariant();
        var md5Hash = Convert.ToHexString(md5.ComputeHash(fileBytes)).ToLowerInvariant();

        using var aes = Aes.Create();
        aes.Key = key;
        aes.GenerateIV();
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        byte[] encryptedBytes;
        using (var encryptor = aes.CreateEncryptor())
        {
            encryptedBytes = encryptor.TransformFinalBlock(fileBytes, 0, fileBytes.Length);
        }

        await File.WriteAllBytesAsync(filePath, encryptedBytes, cancellationToken);
        File.SetAttributes(filePath, File.GetAttributes(filePath) | FileAttributes.ReadOnly);

        return new SavedEvidenceFile(
            filePath,
            sha256Hash,
            md5Hash,
            fileBytes.LongLength,
            Convert.ToBase64String(aes.IV));
    }

    public async Task<byte[]> ReadDecryptedAsync(
        string storedFilePath,
        string encryptionIv,
        CancellationToken cancellationToken)
    {
        var key = Convert.FromBase64String(storageOptions.EncryptionKey);
        if (key.Length != 32)
        {
            throw new InvalidOperationException("Evidence encryption key must be exactly 32 bytes (base64).");
        }

        var encryptedBytes = await File.ReadAllBytesAsync(storedFilePath, cancellationToken);

        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = Convert.FromBase64String(encryptionIv);
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        return decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
    }
}
