using System.Text;
using Renci.SshNet;
using Renci.SshNet.Common;
using Spectre.Console;
using ssharp.Contracts.Services;
using ssharp.Enums;
using ssharp.Models;

namespace ssharp.Services;

public class SshService(
    INotificationService notificationService,
    IHostService hostService,
    IEncryptionService encryptionService) : ISshService
{
    public async Task InitializeAsync()
    {
        AnsiConsole.Clear();
        Ssh? ssh;

        var hosts = await hostService.GetAllHostsAsync();
        var hostNames = hosts.Select(host => host.Ip).ToList();
        hostNames.Add("Create new host");

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a host")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more hosts)[/]")
                .AddChoices(
                    hostNames
                ));

        switch (selection)
        {
            case "Create new host":
                ssh = await CreateNewSshAsync();
                if (null == ssh) return;
                await hostService.SaveHostAsync(ssh);
                await notificationService.SendAsync(new Notification
                (
                    "Success",
                    "Host created successfully",
                    null,
                    LoggingSeverity.Information,
                    null
                ));
                Environment.Exit(0);
                break;
            default:
                ssh = hosts.Find(h => h.Ip == selection);
                SshClient client;
                if (null != ssh?.Password)
                {
                    var password = await encryptionService.DecryptAsync(ssh.Password);
                    client = new SshClient(ssh.Ip, ssh.Port, ssh.Username, password);
                }
                else
                {
                    var privateKey = new PrivateKeyFile(ssh?.PrivateKey,
                        await encryptionService.DecryptAsync(ssh.Passphrase));
                    client = new SshClient(ssh.Ip, ssh.Port, ssh.Username, privateKey);
                }

                await notificationService.SendAsync(new Notification
                (
                    "Connecting",
                    $"Connecting to {ssh.Ip}",
                    null,
                    LoggingSeverity.Information,
                    null
                ));

                await Start(client);
                break;
        }
    }

    private async Task Start(SshClient client)
    {
        AnsiConsole.Clear();
        client.Connect();

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
                case "back":
                    await InitializeAsync();
                    break;
                case "help":
                    AnsiConsole.MarkupLine("[yellow]exit[/] - Exit the shell");
                    AnsiConsole.MarkupLine("[yellow]clear[/] - Clear the console");
                    AnsiConsole.MarkupLine("[yellow]cls[/] - Clear the console");
                    AnsiConsole.MarkupLine("[yellow]help[/] - Display this help message");
                    shell.WriteLine("");
                    break;
                default:
                    if (null != command) shell.WriteLine(command);
                    break;
            }
        }
    }

    private static void DataReceivedHandler(object sender, ShellDataEventArgs e, ShellStream shell)
    {
        if (!sender.Equals(shell)) return;
        var data = Encoding.UTF8.GetString(e.Data);
        Console.Write(data);
    }

    private static void ErrorOccurredHandler(object sender, ExceptionEventArgs e, ShellStream shell)
    {
        if (!sender.Equals(shell)) return;
        AnsiConsole.MarkupLine("[red]Connection closed[/]");
        AnsiConsole.WriteException(e.Exception);
        shell.Close();
    }

    private async Task<Ssh?> CreateNewSshAsync()
    {
        var ip = AnsiConsole.Prompt(new TextPrompt<string>("What is the IP address of your host?").PromptStyle("red"));
        var port = AnsiConsole.Prompt(new TextPrompt<int>("What is the port of your host?").PromptStyle("red"));
        var username =
            AnsiConsole.Prompt(new TextPrompt<string>("What is the username of your host?").PromptStyle("red"));
        var password =
            AnsiConsole.Prompt(
                new TextPrompt<string>(
                        "What is the password of your host (Leave empty if you want to use a private key)")
                    .PromptStyle("red").Secret().AllowEmpty());
        var privateKey =
            AnsiConsole.Prompt(
                new TextPrompt<string>(
                        "Where is the private key of your host? (Leave empty if you want to use a password)")
                    .PromptStyle("red").AllowEmpty());
        var passPhrase =
            AnsiConsole.Prompt(
                new TextPrompt<string>(
                        "What is the passphrase of your private key? (Leave empty if you want to use a password)")
                    .PromptStyle("red").Secret().AllowEmpty());

        if (string.IsNullOrEmpty(password) && string.IsNullOrEmpty(privateKey))
        {
            AnsiConsole.Clear();
            await notificationService.SendAsync(new Notification("Error",
                "You must provide a password or a private key", null, LoggingSeverity.Error, null));
            return null;
        }

        var encryptedPassphrase = await encryptionService.EncryptAsync(passPhrase);

        return new Ssh
        {
            Ip = ip,
            Port = port,
            Username = username,
            PrivateKey = privateKey,
            Passphrase = encryptedPassphrase,
            Password = string.IsNullOrEmpty(password) ? null : await encryptionService.EncryptAsync(password)
        };
    }
}