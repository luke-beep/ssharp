using ssharp.Models;

namespace ssharp.Contracts.Services;

public interface INotificationService
{
    Task<bool> SendAsync(Notification notification);
}