namespace ssharp.Contracts.Services;

public interface ISettingsService
{
    Task<T> ReadSettingAsync<T>(string key);
    Task SaveSettingAsync<T>(string key, T value);
}