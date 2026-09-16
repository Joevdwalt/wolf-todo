using Spectre.Console;
using Wolf.Controls;
using Wolf.Controls.Splash;
using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Features.ApplicationShell.Rendering;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.DayPlanner.Rendering;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ProjectBrowser.Rendering;
using WolfTodo.Tui.Features.Tabs;
using WolfTodo.Tui.Infrastructure.Terminal;
using WolfTodo.Tui.Rendering;

namespace WolfTodo.Tui.Infrastructure;

public sealed class SpectreTerminalUi : ITerminalUi
{
    private readonly Func<int> widthProvider;
    private readonly Func<int> heightProvider;
    private readonly BrowserRenderer browserRenderer;
    private readonly PlannerRenderer plannerRenderer;
    private readonly FocusedTaskRenderer focusedTaskRenderer;
    private readonly TerminalInputReader inputReader;
    private readonly SurfaceThemeRenderer themeRenderer;
    private readonly Func<string> currentDirectoryProvider;
    private readonly Func<DateTimeOffset> animationClock;
    private string? lastRenderedFrame;
    private bool browserRendered;

    public SpectreTerminalUi()
        : this(SafeWindowWidth, SafeWindowHeight, null, null)
    {
    }

    public SpectreTerminalUi(
        Func<int> widthProvider,
        Func<int> heightProvider,
        Func<DateOnly>? todayProvider = null,
        Func<DateTime>? nowProvider = null,
        Func<string>? currentDirectoryProvider = null)
        : this(
            widthProvider,
            heightProvider,
            new BrowserRenderer(widthProvider, heightProvider, todayProvider, nowProvider),
            new PlannerRenderer(widthProvider, heightProvider, todayProvider, nowProvider),
            new TerminalInputReader(),
            new SurfaceThemeRenderer(),
            currentDirectoryProvider,
            nowProvider)
    {
    }

    public SpectreTerminalUi(
        Func<int> widthProvider,
        Func<int> heightProvider,
        BrowserRenderer browserRenderer,
        PlannerRenderer plannerRenderer,
        TerminalInputReader inputReader,
        SurfaceThemeRenderer themeRenderer,
        Func<string>? currentDirectoryProvider = null,
        Func<DateTime>? nowProvider = null,
        Func<DateTimeOffset>? animationClock = null)
    {
        this.widthProvider = widthProvider;
        this.heightProvider = heightProvider;
        this.browserRenderer = browserRenderer;
        this.plannerRenderer = plannerRenderer;
        focusedTaskRenderer = new FocusedTaskRenderer(widthProvider, heightProvider, nowProvider);
        this.inputReader = inputReader;
        this.themeRenderer = themeRenderer;
        this.currentDirectoryProvider = currentDirectoryProvider ?? (() => Environment.CurrentDirectory);
        this.animationClock = animationClock ?? (() => DateTimeOffset.UtcNow);
    }

    public void ShowSplashAndWaitForDismissal(string logo, TuiTheme theme)
    {
        browserRendered = false;
        AnsiConsole.Clear();

        if (widthProvider() < LongestLine(logo) + 2 || heightProvider() < LogoLineCount(logo) + 5)
        {
            AnsiConsole.Write(themeRenderer.OnSurface(
                new Text("Wolf Todo\n", themeRenderer.Style(theme.Heading, Decoration.Bold)),
                theme.Background,
                true));
            AnsiConsole.Write(themeRenderer.OnSurface(
                new Text("Press any key to continue\n", themeRenderer.Style(theme.Muted, Decoration.Dim)),
                theme.Background,
                true));
            inputReader.ReadKey();
            return;
        }

        var startedAt = animationClock();
        var state = SplashBoxState.Create(
            "Wolf Todo",
            "Press any key to continue",
            startedAt,
            logo);
        var controlTheme = ToControlTheme(theme);
        var constraints = new ControlConstraints(
            widthProvider(),
            Math.Max(1, heightProvider() - 1));

        if (!AnsiConsole.Profile.Out.IsTerminal)
        {
            AnsiConsole.Write(themeRenderer.OnSurface(
                SplashBox.Default.Render(state, controlTheme, constraints, startedAt + SplashBox.ExpansionDuration),
                theme.Background,
                true));
            inputReader.ReadKey();
            return;
        }

        AnsiConsole.Live(new Text(string.Empty))
            .AutoClear(false)
            .Overflow(VerticalOverflow.Crop)
            .Start(context =>
            {
                while (true)
                {
                    var now = animationClock();
                    context.UpdateTarget(themeRenderer.OnSurface(
                        SplashBox.Default.Render(state, controlTheme, constraints, now),
                        theme.Background,
                        true));
                    context.Refresh();

                    var nextFrame = SplashBox.Default.NextFrameAt(state, now);
                    if (nextFrame is null)
                    {
                        inputReader.ReadKey();
                        return;
                    }

                    var wait = nextFrame.Value - now;
                    if (wait > TimeSpan.Zero && inputReader.ReadKey(wait) is not null)
                    {
                        return;
                    }
                }
            });
    }

    public void ShowBrowser(TabStripView tabs, BrowserView view, TuiKeyBindings keyBindings) =>
        ShowBrowser(tabs, view, keyBindings, TuiThemes.Wolf);

    public void ShowBrowser(
        TabStripView tabs,
        BrowserView view,
        TuiKeyBindings keyBindings,
        TuiTheme theme)
    {
        CaptureFrame(() =>
        {
            var useSynchronizedUpdate = BeginFrame();
            browserRenderer.ShowBrowser(tabs, view, keyBindings, theme);
            EndFrame(useSynchronizedUpdate);
        });
    }

    public void ShowPlanner(
        TabStripView tabs,
        PlannerView view,
        TuiKeyBindings keyBindings,
        TuiTheme theme)
    {
        CaptureFrame(() =>
        {
            var useSynchronizedUpdate = BeginFrame();
            plannerRenderer.ShowPlanner(tabs, view, keyBindings, theme);
            EndFrame(useSynchronizedUpdate);
        });
    }

    public void ShowFocusedTask(
        FocusedTaskView view,
        TuiKeyBindings keyBindings,
        TuiTheme theme)
    {
        CaptureFrame(() =>
        {
            var useSynchronizedUpdate = BeginFrame();
            focusedTaskRenderer.ShowFocusedTask(view, keyBindings, theme);
            EndFrame(useSynchronizedUpdate);
        });
    }

    public void ShowTaskLinkPanel(TaskLinkPanelState panel, TuiTheme theme)
    {
        CaptureFrame(() =>
        {
            var synchronized = BeginFrame();
            AnsiConsole.Clear();
            AnsiConsole.Write(TaskLinkPanelRenderer.Render(panel, theme, Math.Max(3, widthProvider())));
            EndFrame(synchronized);
        });
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

    public ScreenDumpResult DumpScreen()
    {
        if (lastRenderedFrame is null)
        {
            return new ScreenDumpResult(null, "No application screen has been rendered yet.");
        }

        try
        {
            var directory = Path.Combine(currentDirectoryProvider(), "screen-dumps");
            Directory.CreateDirectory(directory);
            var path = Path.Combine(directory, $"wolf-todo-screen-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
            File.WriteAllText(path, lastRenderedFrame.Replace("\r\n", "\n"));
            return new ScreenDumpResult(path, null);
        }
        catch (Exception exception)
        {
            return new ScreenDumpResult(null, $"Could not dump screen: {exception.Message}");
        }
    }

    public ConsoleKeyInfo ReadKey() => inputReader.ReadKey();

    public ConsoleKeyInfo? ReadKey(TimeSpan timeout) => inputReader.ReadKey(timeout);

    private void CaptureFrame(Action render)
    {
        var liveConsole = AnsiConsole.Console;
        using var recorder = liveConsole.CreateRecorder();
        AnsiConsole.Console = recorder;

        try
        {
            render();
            lastRenderedFrame = recorder.ExportText();
        }
        finally
        {
            AnsiConsole.Console = liveConsole;
        }
    }

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
        .Split(['\r', '\n'], StringSplitOptions.None)
        .Max(line => line.Length);

    private static int LogoLineCount(string content) => content
        .TrimEnd('\r', '\n')
        .Split(['\r', '\n'], StringSplitOptions.None)
        .Length;

    private static ControlTheme ToControlTheme(TuiTheme theme) => new(
        theme.Text,
        theme.Muted,
        theme.Accent,
        theme.Heading,
        theme.Border,
        theme.BorderActive,
        theme.Surface,
        theme.Success,
        theme.Warning,
        theme.Error);
}
