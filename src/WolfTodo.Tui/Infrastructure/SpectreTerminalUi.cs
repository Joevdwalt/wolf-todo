using Spectre.Console;
using WolfTodo.Tui.Features.Splash;

namespace WolfTodo.Tui.Infrastructure;

public sealed class SpectreTerminalUi : ITerminalUi
{
    public void ShowSplash(string logo)
    {
        AnsiConsole.Clear();

        var content = new Rows(
            new Text(logo),
            new Text(string.Empty),
            new Text("Wolf Todo"),
            new Text("Press any key to continue"));

        if (Console.WindowWidth < LongestLine(logo) || Console.WindowHeight < 5)
        {
            AnsiConsole.WriteLine("Wolf Todo");
            AnsiConsole.WriteLine("Press any key to continue");
            return;
        }

        AnsiConsole.Write(new Align(content, HorizontalAlignment.Center, VerticalAlignment.Middle));
    }

    public void ShowHome(HomeScreenState state)
    {
        AnsiConsole.Clear();

        var commandLine = state.IsCommandMode ? state.Command : "Press : to enter a command";
        var content = new Rows(
            new Text("Wolf Todo"),
            new Text(string.Empty),
            new Text("Todo manager coming soon"),
            new Text(string.Empty),
            new Text(commandLine),
            new Text(state.Error ?? string.Empty));

        if (Console.WindowWidth < "Todo manager coming soon".Length || Console.WindowHeight < 6)
        {
            AnsiConsole.WriteLine("Wolf Todo");
            AnsiConsole.WriteLine("Todo manager coming soon");
            AnsiConsole.WriteLine(commandLine);

            if (state.Error is not null)
            {
                AnsiConsole.WriteLine(state.Error);
            }

            return;
        }

        AnsiConsole.Write(new Align(content, HorizontalAlignment.Center, VerticalAlignment.Middle));
    }

    public void ShowStartupError(string message)
    {
        AnsiConsole.MarkupLine($"[red]Startup error:[/] {Markup.Escape(message)}");
    }

    public ConsoleKeyInfo ReadKey() => Console.ReadKey(intercept: true);

    private static int LongestLine(string content) => content
        .Split(Environment.NewLine, StringSplitOptions.None)
        .Max(line => line.Length);
}
