using Renci.SshNet;

namespace ssharp.Contracts.Services;

public interface ISshService
{
    Task<SshClient> InitializeAsync();
}