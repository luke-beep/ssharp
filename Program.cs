using System.Text;
using Renci.SshNet;
using Renci.SshNet.Common;
using Spectre.Console;
using ssharp.Contracts.Services;
using ssharp.Enums;
using ssharp.Services;

namespace ssharp;

internal class Program
{
    private static async Task Main(string[] args)
    {
        AnsiConsole.Clear();

        ILoggingService loggingService = new LoggingService();
        INotificationService notificationService = new NotificationService();
        IFileService fileService = new FileService();
        ISettingsService settingsService = new SettingsService(fileService);
        IHostService hostService = new HostService(fileService);
        IEncryptionService encryptionService = new EncryptionService();
        ISshService sshService = new SshService(fileService, notificationService, settingsService, hostService,
            encryptionService);

        AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            ExceptionHandler((Exception)eventArgs.ExceptionObject, loggingService);

        await Connection(sshService);
    }

    private static async Task Connection(ISshService sshService)
    {
        var text = new FigletText("ssharper");
        text.Justification = Justify.Center;
        AnsiConsole.Clear();
        AnsiConsole.Write(text);
        var client = await sshService.InitializeAsync();
        

        IDictionary<TerminalModes, uint> options = new Dictionary<TerminalModes, uint>
        {
            { TerminalModes.ECHO, 0 },
            { TerminalModes.ECHOCTL, 0 },
            { TerminalModes.ECHOE, 0 },
            { TerminalModes.ECHOK, 0 },
            { TerminalModes.ECHOKE, 0 },
            { TerminalModes.ECHONL, 0 },
            { TerminalModes.ICANON, 1 },
            { TerminalModes.IEXTEN, 0 },
            { TerminalModes.ISIG, 1 },
            { TerminalModes.NOFLSH, 0 },
            { TerminalModes.TOSTOP, 0 },
            { TerminalModes.TTY_OP_ISPEED, 14400 },
            { TerminalModes.TTY_OP_OSPEED, 14400 },
            { TerminalModes.IXON, 1 }
        };

        client.Connect();
        var shell = client.CreateShellStream(Environment.UserName, 80, 24, 800, 600, 1024, options);

        shell.DataReceived += (sender, e) => DataReceivedHandler(sender, e, shell);
        shell.ErrorOccurred += (sender, e) => ErrorOccurredHandler(sender, e, shell);
        shell.Closed += (sender, e) => client.Disconnect();

        while (shell.CanWrite)
        {
            var command = Console.ReadLine();
            switch (command)
            {
                case "exit":
                    Environment.Exit(0);
                    break;
                case "clear":
                    AnsiConsole.Clear();
                    shell.WriteLine("");
                    break;
                case "cls":
                    AnsiConsole.Clear();
                    shell.WriteLine("");
                    break;
                case "help":
                    AnsiConsole.MarkupLine("[yellow]exit[/] - Exit the shell");
                    AnsiConsole.MarkupLine("[yellow]clear[/] - Clear the console");
                    AnsiConsole.MarkupLine("[yellow]cls[/] - Clear the console");
                    AnsiConsole.MarkupLine("[yellow]help[/] - Display this help message");
                    shell.WriteLine("");
                    break;
                case "back":
                    await Connection(sshService);
                    return;
                default:
                    shell.WriteLine(command);
                    break;
            }
        }

    }

    private static void DataReceivedHandler(object sender, ShellDataEventArgs e, ShellStream shell)
    {
        var data = Encoding.UTF8.GetString(e.Data);
        Console.Write(data);
    }

    private static void ErrorOccurredHandler(object sender, ExceptionEventArgs e, ShellStream shell)
    {
        AnsiConsole.WriteException(e.Exception);
        shell.Close();
    }

    private static void ExceptionHandler(Exception exception, ILoggingService loggingService)
    {
        loggingService.LogAsync(exception.Message, LoggingSeverity.Error, exception);
        AnsiConsole.WriteException(exception);
    }
}