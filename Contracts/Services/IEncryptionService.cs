namespace ssharp.Contracts.Services;

public interface IEncryptionService
{
    Task<byte[]> EncryptAsync(string password);
    Task<string> DecryptAsync(byte[] password);
}