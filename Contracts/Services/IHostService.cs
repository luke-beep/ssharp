using ssharp.Models;

namespace ssharp.Contracts.Services;

public interface IHostService
{
    Task<List<Ssh>> GetAllHostsAsync();
    Task<T> ReadHostAsync<T>(string key);
    Task SaveHostAsync(Ssh? ssh);
}