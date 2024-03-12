using ssharp.Enums;

namespace ssharp.Contracts.Services;

public interface ILoggingService
{
    Task<bool> LogAsync(string message, LoggingSeverity severity, Exception? exception);
}