using ssharp.Contracts.Services;
using ssharp.Helpers;

namespace ssharp.Services;

public class SettingsService : ISettingsService
{
    private const string DefaultApplicationDataFolder = "ssharp";
    private const string DefaultLocalSettingsFile = "settings.json";

    private readonly string _applicationDataFolder;

    private readonly IFileService _fileService;

    private readonly string _localApplicationData =
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    private readonly string _localsettingsFile;

    private bool _isInitialized;

    private IDictionary<string, object> _settings;

    public SettingsService(IFileService fileService)
    {
        _fileService = fileService;

        _applicationDataFolder = Path.Combine(_localApplicationData, DefaultApplicationDataFolder);
        _localsettingsFile = DefaultLocalSettingsFile;
        _settings = new Dictionary<string, object>();
    }

    public async Task<T> ReadSettingAsync<T>(string key)
    {
        await InitializeAsync();

        if (_settings != null && _settings.TryGetValue(key, out var obj))
            return await Json.ToObjectAsync<T>((string)obj);


        return default;
    }

    public async Task SaveSettingAsync<T>(string key, T value)
    {
        await InitializeAsync();

        _settings[key] = await Json.StringifyAsync(value);

        await Task.Run(() => _fileService.Save(_applicationDataFolder, _localsettingsFile, _settings));
    }

    private async Task InitializeAsync()
    {
        if (!_isInitialized)
        {
            _settings = await Task.Run(() =>
                            _fileService.Read<IDictionary<string, object>>(_applicationDataFolder,
                                _localsettingsFile)) ??
                        new Dictionary<string, object>();

            _isInitialized = true;
        }
    }
}