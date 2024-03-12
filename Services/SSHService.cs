using Renci.SshNet;
using Spectre.Console;
using ssharp.Contracts.Services;
using ssharp.Enums;
using ssharp.Models;

namespace ssharp.Services;

public class SshService(
    IFileService fileService,
    INotificationService notificationService,
    ISettingsService settingsService,
    IHostService hostService,
    IEncryptionService encryptionService) : ISshService
{
    public async Task<SshClient?> InitializeAsync()
    {
        var hosts = await hostService.GetAllHostsAsync();
        var hostSelection = hosts.Select(host => host.Ip).ToList();
        hostSelection.Add("Create new host");

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Select a host")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more hosts)[/]")
                .AddChoices(
                    hostSelection
                ));
        switch (selection)
        {
            case "Create new host":
                var ssh = await CreateNewSshAsync();
                await hostService.SaveHostAsync(ssh);
                return null;
            default:
                var selectedHost = hosts.Find(h => h.Ip == selection);
                SshClient client;
                if (selectedHost?.Password != null)
                {
                    var password = await encryptionService.DecryptAsync(selectedHost.Password);
                    client = new SshClient(selectedHost.Ip, selectedHost.Port, selectedHost.Username, password);
                }
                else
                {
                    var privateKey = new PrivateKeyFile(selectedHost?.PrivateKey, await encryptionService.DecryptAsync(selectedHost.Passphrase));
                    client = new SshClient(selectedHost.Ip, selectedHost.Port, selectedHost.Username, privateKey);
                }

                return client;
        }
    }

    private async Task<Ssh?> CreateNewSshAsync()
    {
        var ip = AnsiConsole.Prompt(
            new TextPrompt<string>(
                    "What is the IP address of your host?")
                .PromptStyle("red")
        );
        var port = AnsiConsole.Prompt(
            new TextPrompt<int>(
                    "What is the port of your host?")
                .PromptStyle("red")
        );
        var username = AnsiConsole.Prompt(
            new TextPrompt<string>(
                    "What is the username of your host?")
                .PromptStyle("red")
        );
        var password = AnsiConsole.Prompt(
            new TextPrompt<string>(
                    "What is the password of your host (Leave empty if you want to use a private key)")
                .PromptStyle("red")
                .Secret()
                .AllowEmpty()
        );
        var privateKey = AnsiConsole.Prompt(
            new TextPrompt<string>(
                    "Where is the private key of your host? (Leave empty if you want to use a password)")
                .PromptStyle("red")
                .AllowEmpty()
        );
        var passPhrase = AnsiConsole.Prompt(
                       new TextPrompt<string>(
                                              "What is the passphrase of your private key? (Leave empty if you want to use a password)")
                                      .PromptStyle("red")
                                      .Secret()
                                      .AllowEmpty()
                              );
        if (string.IsNullOrEmpty(password) && string.IsNullOrEmpty(privateKey))
        {
            await notificationService.SendAsync(new Notification
            (
                "Error",
                "You must provide a password or a private key",
                null,
                LoggingSeverity.Error,
                null
            ));
            return null;
        }

        var encryptedPassphrase = await encryptionService.EncryptAsync(passPhrase);
        if (string.IsNullOrEmpty(password))
        {
            return new Ssh
            {
                Ip = ip,
                Port = port,
                Username = username,
                PrivateKey = privateKey,
                Passphrase = encryptedPassphrase
            };
        }

        var encryptedPassword = await encryptionService.EncryptAsync(password);
        return new Ssh
        {
            Ip = ip,
            Port = port,
            Username = username,
            Password = encryptedPassword
        };

    }
}