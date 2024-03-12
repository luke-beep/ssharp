using ssharp.Contracts.Services;
using ssharp.Enums;
using ssharp.Helpers;
using ssharp.Models;

namespace ssharp.Services;

public class LoggingService : ILoggingService
{
    private const string DefaultApplicationDataFolder = "ssharp\\Logs";

    private readonly string _localApplicationData =
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    private readonly string _applicationDataFolder;

    private bool _isInitialized;

    public LoggingService()
    {
        _applicationDataFolder = Path.Combine(_localApplicationData, DefaultApplicationDataFolder);
    }

    private async Task InitializeAsync()
    {
        if (!_isInitialized)
        {
            if (!Directory.Exists(_applicationDataFolder))
            {
                Directory.CreateDirectory(_applicationDataFolder);
            }

            _isInitialized = true;
        }
    }

    public async Task<bool> LogAsync(string message, LoggingSeverity severity, Exception? exception)
    {
        await InitializeAsync();

        var log = new Log
        {
            Message = message,
            Severity = severity,
            Exception = exception
        };

        var logFile = Path.Combine(_applicationDataFolder, $"{DateTime.Now:yyyy-MM-dd}.json");
        await using var writer = new StreamWriter(logFile, true);
        await writer.WriteLineAsync(await Json.StringifyAsync(log));

        return true;
    }
}