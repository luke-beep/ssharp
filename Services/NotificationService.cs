using Spectre.Console;
using ssharp.Contracts.Services;
using ssharp.Models;

namespace ssharp.Services;

public class NotificationService : INotificationService
{
    public async Task<bool> SendAsync(Notification notification)
    {
        var rows = new List<Text>
        {
            new(notification.Title, new Style(Color.Red, Color.Black)),
            new(notification.Message, new Style(Color.Green, Color.Black)),
            new(notification.Severity.ToString(), new Style(Color.Blue, Color.Black))
        };
        RenderNotification(rows);
        await Task.Delay(2000);
        ClearNotification(rows);
        return true;
    }

    private static void RenderNotification(List<Text> rows)
    {
        var (cy, cx) = Console.GetCursorPosition();
        var i = 0;
        foreach (var row in rows)
        {
            Console.SetCursorPosition(Console.WindowWidth - row.Length, i);
            i += 1;
            AnsiConsole.Write(row);
        }

        Console.SetCursorPosition(cx, cy);
    }

    private static void ClearNotification(List<Text> rows)
    {
        var (cy, cx) = Console.GetCursorPosition();
        var i = 0;
        foreach (var row in rows)
        {
            Console.SetCursorPosition(Console.WindowWidth - row.Length, i);
            i += 1;
            AnsiConsole.Write(new string(' ', row.Length));
        }

        Console.SetCursorPosition(cx, cy);
    }
}