using ssharp.Enums;

namespace ssharp.Models;

public class Notification(string title, string message, object? data, LoggingSeverity severity, Exception? exception)
{
    public string Title { get; set; } = title;
    public string Message { get; set; } = message;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public object? Data { get; set; } = data;
    public LoggingSeverity Severity { get; set; } = severity;
    public Exception? Exception { get; set; } = exception;
}