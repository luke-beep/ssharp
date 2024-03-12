using ssharp.Contracts.Services;
using System.Security.Cryptography;
using System.Text;

namespace ssharp.Services;

public class EncryptionService : IEncryptionService
{
    private readonly byte[] _iv =
    [
        0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x09, 0x10, 0x11, 0x12, 0x13, 0x14, 0x15, 0x16
    ];

    private static readonly string Key = Environment.UserName;

    public async Task<byte[]> EncryptAsync(string password)
    {
        using var aes = Aes.Create();
        aes.Key = DeriveKeyFromPassword();
        aes.IV = _iv;

        using MemoryStream output = new();
        await using CryptoStream cryptoStream = new(output, aes.CreateEncryptor(), CryptoStreamMode.Write);

        await cryptoStream.WriteAsync(Encoding.Unicode.GetBytes(password));
        await cryptoStream.FlushFinalBlockAsync();

        return output.ToArray();
    }
    public async Task<string> DecryptAsync(byte[] encrypted)
    {
        using var aes = Aes.Create();
        aes.Key = DeriveKeyFromPassword();
        aes.IV = _iv;

        using MemoryStream input = new(encrypted);
        await using CryptoStream cryptoStream = new(input, aes.CreateDecryptor(), CryptoStreamMode.Read);

        using MemoryStream output = new();
        await cryptoStream.CopyToAsync(output);

        return Encoding.Unicode.GetString(output.ToArray());
    }

    private static byte[] DeriveKeyFromPassword()
    {
        var emptySalt = Array.Empty<byte>();
        const int iterations = 1000;
        const int desiredKeyLength = 16;
        var hashMethod = HashAlgorithmName.SHA384;
        return Rfc2898DeriveBytes.Pbkdf2(Encoding.Unicode.GetBytes(Key),
            emptySalt,
            iterations,
            hashMethod,
            desiredKeyLength);
    }
}