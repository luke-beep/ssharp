using ssharp.Enums;

namespace ssharp.Models;

public class Log
{
    public string Message { get; set; }
    public LoggingSeverity Severity { get; set; }
    public Exception? Exception { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.Now;
}