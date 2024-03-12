using ssharp.Contracts.Services;
using Newtonsoft.Json.Linq;
using ssharp.Helpers;
using ssharp.Models;

namespace ssharp.Services;

public class HostService : IHostService
{
    private const string DefaultApplicationDataFolder = "ssharp";
    private const string DefaultLocalHostsFile = "hosts.json";

    private readonly IFileService _fileService;

    private readonly string _localApplicationData =
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    private readonly string _applicationDataFolder;
    private readonly string _localhostsFile;

    private IDictionary<string, object> _settings;

    private bool _isInitialized;
    public HostService(IFileService fileService)
    {
        _fileService = fileService;

        _applicationDataFolder = Path.Combine(_localApplicationData, DefaultApplicationDataFolder);
        _localhostsFile = DefaultLocalHostsFile;
        _settings = new Dictionary<string, object>();
    }

    private async Task InitializeAsync()
    {
        if (!_isInitialized)
        {
            _settings = await Task.Run(() =>
                            _fileService.Read<IDictionary<string, object>>(_applicationDataFolder,
                                _localhostsFile)) ??
                        new Dictionary<string, object>();

            _isInitialized = true;
        }
    }

    public async Task<List<Ssh>> GetAllHostsAsync()
    {
        await InitializeAsync();
        var hosts = new List<Ssh>();

        foreach (var (key, value) in _settings)
        {
            var obj = (JObject)value;
            var port = obj["port"].ToString();
            var user = obj["user"].ToString();
            var password = obj["password"]?.ToObject<byte[]>();
            var privateKeyFile = obj["privateKey"]?.ToString();
            var passphrase = obj["passphrase"]?.ToObject<byte[]>();
            var ssh = new Ssh
            {
                Ip = key,
                Port = int.Parse(port),
                Username = user,
                Password = password,
                PrivateKey = privateKeyFile,
                Passphrase = passphrase
            };
            hosts.Add(ssh);
        }
        return hosts;
    }

    public async Task<T> ReadHostAsync<T>(string name)
    {
        await InitializeAsync();

        if (_settings != null && _settings.TryGetValue(name, out var obj))
        {
            return await Json.ToObjectAsync<T>((string)obj);
        }

        return default;
    }

    public async Task SaveHostAsync(Ssh ssh)
    {
        await InitializeAsync();

        var obj = new Dictionary<string, object>
        {
            {"port", ssh.Port.ToString()},
            {"user", ssh.Username},
            {"password", ssh.Password},
            {"privateKey", ssh.PrivateKey},
            {"passphrase", ssh.Passphrase}
        };

        _settings[ssh.Ip] = obj;

        await Task.Run(() => _fileService.Save(_applicationDataFolder, _localhostsFile, _settings));
    }
}