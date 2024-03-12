namespace ssharp.Models;

public class Ssh
{
    public required string Ip { get; set; }
    public int Port { get; set; } = 22;
    public required string Username { get; set; }
    public byte[]? Password { get; set; } = null;
    public string? PrivateKey { get; set; } = null;
    public byte[]? Passphrase { get; set; } = null;
}