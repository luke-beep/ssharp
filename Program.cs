using Spectre.Console;
using ssharp.Contracts.Services;
using ssharp.Enums;
using ssharp.Services;

namespace ssharp;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // Services
        ILoggingService loggingService = new LoggingService();
        INotificationService notificationService = new NotificationService();
        IFileService fileService = new FileService();
        ISettingsService settingsService = new SettingsService(fileService);
        IHostService hostService = new HostService(fileService);
        IEncryptionService encryptionService = new EncryptionService();
        ISshService sshService = new SshService(notificationService, hostService,
            encryptionService);

        // Exception handling
        AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            ExceptionHandler((Exception)eventArgs.ExceptionObject, loggingService);

        // Start
        await sshService.InitializeAsync();
    }

    private static void ExceptionHandler(Exception exception, ILoggingService loggingService)
    {
        loggingService.LogAsync(exception.Message, LoggingSeverity.Error, exception);
        AnsiConsole.WriteException(exception);
    }
}