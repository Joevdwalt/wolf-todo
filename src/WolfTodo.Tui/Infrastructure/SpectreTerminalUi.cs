using Spectre.Console;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.DayPlanner.Rendering;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ProjectBrowser.Rendering;
using WolfTodo.Tui.Features.Splash;
using WolfTodo.Tui.Features.Tabs;
using WolfTodo.Tui.Rendering;

namespace WolfTodo.Tui.Infrastructure;

public sealed class SpectreTerminalUi : ITerminalUi
{
    private readonly Func<int> widthProvider;
    private readonly Func<int> heightProvider;
    private readonly BrowserRenderer browserRenderer;
    private readonly PlannerRenderer plannerRenderer;
    private readonly TerminalInputReader inputReader;
    private readonly SurfaceThemeRenderer themeRenderer;
    private bool browserRendered;

    public SpectreTerminalUi()
        : this(SafeWindowWidth, SafeWindowHeight, null, null)
    {
    }

    public SpectreTerminalUi(
        Func<int> widthProvider,
        Func<int> heightProvider,
        Func<DateOnly>? todayProvider = null,
        Func<DateTime>? nowProvider = null)
        : this(
            widthProvider,
            heightProvider,
            new BrowserRenderer(widthProvider, heightProvider, todayProvider, nowProvider),
            new PlannerRenderer(widthProvider, heightProvider, todayProvider, nowProvider),
            new TerminalInputReader(),
            new SurfaceThemeRenderer())
    {
    }

    public SpectreTerminalUi(
        Func<int> widthProvider,
        Func<int> heightProvider,
        BrowserRenderer browserRenderer,
        PlannerRenderer plannerRenderer,
        TerminalInputReader inputReader,
        SurfaceThemeRenderer themeRenderer)
    {
        this.widthProvider = widthProvider;
        this.heightProvider = heightProvider;
        this.browserRenderer = browserRenderer;
        this.plannerRenderer = plannerRenderer;
        this.inputReader = inputReader;
        this.themeRenderer = themeRenderer;
    }

    public void ShowSplash(string logo) => ShowSplash(logo, TuiThemes.Wolf);

    public void ShowSplash(string logo, TuiTheme theme)
    {
        browserRendered = false;
        AnsiConsole.Clear();

        var content = new Rows(
            new Text(logo, themeRenderer.Style(theme.Accent)),
            new Text(string.Empty),
            new Text("Wolf Todo", themeRenderer.Style(theme.Heading, Decoration.Bold)),
            new Text("Press any key to continue", themeRenderer.Style(theme.Muted, Decoration.Dim)));

        if (widthProvider() < LongestLine(logo) || heightProvider() < 5)
        {
            AnsiConsole.Write(themeRenderer.OnSurface(
                new Text("Wolf Todo\n", themeRenderer.Style(theme.Heading, Decoration.Bold)),
                theme.Background,
                true));
            AnsiConsole.Write(themeRenderer.OnSurface(
                new Text("Press any key to continue\n", themeRenderer.Style(theme.Muted, Decoration.Dim)),
                theme.Background,
                true));
            return;
        }

        AnsiConsole.Write(themeRenderer.OnSurface(
            new Align(content, HorizontalAlignment.Center, VerticalAlignment.Middle),
            theme.Background,
            true));
    }

    public void ShowBrowser(TabStripView tabs, BrowserView view, TuiKeyBindings keyBindings) =>
        ShowBrowser(tabs, view, keyBindings, TuiThemes.Wolf);

    public void ShowBrowser(
        TabStripView tabs,
        BrowserView view,
        TuiKeyBindings keyBindings,
        TuiTheme theme)
    {
        var useSynchronizedUpdate = BeginFrame();
        browserRenderer.ShowBrowser(tabs, view, keyBindings, theme);
        EndFrame(useSynchronizedUpdate);
    }

    public void ShowPlanner(
        TabStripView tabs,
        PlannerView view,
        TuiKeyBindings keyBindings,
        TuiTheme theme)
    {
        var useSynchronizedUpdate = BeginFrame();
        plannerRenderer.ShowPlanner(tabs, view, keyBindings, theme);
        EndFrame(useSynchronizedUpdate);
    }

    public void ShowStartupError(string message)
    {
        AnsiConsole.MarkupLine($"[red]Startup error:[/] {Markup.Escape(message)}");
    }

    public void SetCursorVisible(bool visible)
    {
        if (!AnsiConsole.Profile.Out.IsTerminal)
        {
            return;
        }

        var writer = AnsiConsole.Profile.Out.Writer;
        writer.Write(visible ? "\u001b[?25h" : "\u001b[?25l");
        writer.Flush();
    }

    public void SuspendForExternalProcess()
    {
        SetCursorVisible(true);
        browserRendered = false;
        AnsiConsole.Clear();
    }

    public void ResumeAfterExternalProcess()
    {
        browserRendered = false;
        AnsiConsole.Clear();
        SetCursorVisible(false);
    }

    public void RingBell()
    {
        var writer = AnsiConsole.Profile.Out.Writer;
        writer.Write('\a');
        writer.Flush();
    }

    public ConsoleKeyInfo ReadKey() => inputReader.ReadKey();

    public ConsoleKeyInfo? ReadKey(TimeSpan timeout) => inputReader.ReadKey(timeout);

    private bool BeginFrame()
    {
        var useSynchronizedUpdate = browserRendered && AnsiConsole.Profile.Out.IsTerminal;
        if (browserRendered)
        {
            BeginUpdate(useSynchronizedUpdate);
        }
        else
        {
            AnsiConsole.Clear();
            browserRendered = true;
        }

        return useSynchronizedUpdate;
    }

    private static void BeginUpdate(bool synchronized)
    {
        if (!AnsiConsole.Profile.Out.IsTerminal)
        {
            return;
        }

        var writer = AnsiConsole.Profile.Out.Writer;

        if (synchronized)
        {
            writer.Write("\u001b[?2026h");
        }

        writer.Write("\u001b[H");
    }

    private static void EndFrame(bool synchronized)
    {
        if (!AnsiConsole.Profile.Out.IsTerminal)
        {
            return;
        }

        var writer = AnsiConsole.Profile.Out.Writer;
        writer.Write("\u001b[J");

        if (synchronized)
        {
            writer.Write("\u001b[?2026l");
        }

        writer.Flush();
    }

    private static int SafeWindowWidth()
    {
        try
        {
            return Console.WindowWidth;
        }
        catch (IOException)
        {
            return 80;
        }
    }

    private static int SafeWindowHeight()
    {
        try
        {
            return Console.WindowHeight;
        }
        catch (IOException)
        {
            return 24;
        }
    }

    private static int LongestLine(string content) => content
        .Split(Environment.NewLine, StringSplitOptions.None)
        .Max(line => line.Length);
}
