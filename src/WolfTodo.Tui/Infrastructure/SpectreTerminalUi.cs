using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Splash;

namespace WolfTodo.Tui.Infrastructure;

public sealed class SpectreTerminalUi : ITerminalUi
{
    private readonly Func<int> widthProvider;
    private readonly Func<int> heightProvider;
    private bool browserRendered;

    public SpectreTerminalUi() : this(SafeWindowWidth, SafeWindowHeight)
    {
    }

    public SpectreTerminalUi(Func<int> widthProvider, Func<int> heightProvider)
    {
        this.widthProvider = widthProvider;
        this.heightProvider = heightProvider;
    }

    public void ShowSplash(string logo)
    {
        browserRendered = false;
        AnsiConsole.Clear();

        var content = new Rows(
            new Text(logo),
            new Text(string.Empty),
            new Text("Wolf Todo"),
            new Text("Press any key to continue"));

        if (widthProvider() < LongestLine(logo) || heightProvider() < 5)
        {
            AnsiConsole.WriteLine("Wolf Todo");
            AnsiConsole.WriteLine("Press any key to continue");
            return;
        }

        AnsiConsole.Write(new Align(content, HorizontalAlignment.Center, VerticalAlignment.Middle));
    }

    public void ShowBrowser(BrowserView view)
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

        var width = widthProvider();
        var height = heightProvider();

        if (width >= 120 && height >= 24)
        {
            WriteWide(view, width);
        }
        else if (width >= 80 && height >= 18)
        {
            WriteMedium(view, width);
        }
        else
        {
            WriteNarrow(view);
        }

        WriteStatus(view, width < 80 || height < 18);
        EndUpdate(useSynchronizedUpdate);
    }

    public void ShowStartupError(string message)
    {
        AnsiConsole.MarkupLine($"[red]Startup error:[/] {Markup.Escape(message)}");
    }

    public ConsoleKeyInfo ReadKey() => Console.ReadKey(intercept: true);

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

    private static void EndUpdate(bool synchronized)
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

    private static void WriteWide(BrowserView view, int terminalWidth)
    {
        const int projectWidth = 22;
        const int frameAndPaddingWidth = 10;
        var remainingWidth = terminalWidth - projectWidth - frameAndPaddingWidth;
        var todoWidth = remainingWidth / 2;
        var detailWidth = remainingWidth - todoWidth;
        var table = CreatePaneTable(
            ("Projects", projectWidth, view.State.Focus == BrowserFocus.Projects),
            ($"Todos: {view.SelectedProjectTitle}", todoWidth, view.State.Focus == BrowserFocus.Todos),
            ("Details", detailWidth, view.State.Focus == BrowserFocus.Details));
        table.AddRow(
            ProjectContent(view),
            TodoContent(view),
            DetailContent(view));
        AnsiConsole.Write(table);
    }

    private static void WriteMedium(BrowserView view, int terminalWidth)
    {
        const int projectWidth = 22;
        const int frameAndPaddingWidth = 7;
        var contentWidth = terminalWidth - projectWidth - frameAndPaddingWidth;
        var showDetails = view.State.Focus == BrowserFocus.Details;
        var table = CreatePaneTable(
            ("Projects", projectWidth, view.State.Focus == BrowserFocus.Projects),
            (showDetails ? "Details" : $"Todos: {view.SelectedProjectTitle}", contentWidth, true));
        table.AddRow(
            ProjectContent(view),
            showDetails ? DetailContent(view) : TodoContent(view));
        AnsiConsole.Write(table);
    }

    private static void WriteNarrow(BrowserView view)
    {
        var title = view.State.Focus switch
        {
            BrowserFocus.Projects => "Projects",
            BrowserFocus.Todos => $"Todos: {view.SelectedProjectTitle}",
            _ => "Details"
        };
        var content = view.State.Focus switch
        {
            BrowserFocus.Projects => ProjectContent(view),
            BrowserFocus.Todos => TodoContent(view),
            _ => DetailContent(view)
        };
        var table = CreatePaneTable((title, null, true));
        table.AddRow(content);

        AnsiConsole.Write(table);
    }

    private static Table CreatePaneTable(params (string Title, int? Width, bool Focused)[] panes)
    {
        var table = new Table().RoundedBorder().Expand();

        foreach (var pane in panes)
        {
            var header = pane.Focused
                ? new Markup($"[cyan bold]{Markup.Escape(pane.Title)}[/]")
                : new Markup($"[bold]{Markup.Escape(pane.Title)}[/]");
            table.AddColumn(new TableColumn(header)
            {
                Width = pane.Width,
                NoWrap = false,
                Padding = new Padding(1, 0)
            });
        }

        return table;
    }

    private static IRenderable ProjectContent(BrowserView view)
    {
        var lines = view.Projects.Select(row =>
        {
            var cursor = row.IsSelected ? ">" : " ";
            var error = row.Error is null ? " " : "!";
            var count = row.Error is null ? $" {row.ActiveCount}" : string.Empty;
            return (IRenderable)new Text($"{cursor}{error} {row.Title}{count}").Ellipsis();
        });

        return CreateContent(lines);
    }

    private static IRenderable TodoContent(BrowserView view)
    {
        IEnumerable<IRenderable> lines;

        if (view.Diagnostic is not null)
        {
            lines = [new Text("Select the error entry for details.")];
        }
        else if (view.Todos.Length == 0)
        {
            lines = [new Text(view.EmptyMessage)];
        }
        else
        {
            lines = view.Todos.Select(row => row.Heading is not null
                ? (IRenderable)new Markup($"[bold]{Markup.Escape(row.Heading)}[/]")
                : new Text(FormatTodo(row)).Ellipsis());
        }

        return CreateContent(lines);
    }

    private static IRenderable DetailContent(BrowserView view)
    {
        var lines = new List<IRenderable>();

        if (view.Diagnostic is not null)
        {
            lines.Add(new Markup("[red bold]Project error[/]"));
            lines.Add(new Text(view.SelectedProjectPath ?? string.Empty));
            lines.Add(new Text(string.Empty));
            lines.Add(new Text(view.Diagnostic));
        }
        else if (view.SelectedTodo is null)
        {
            lines.Add(new Text(view.EmptyMessage));
        }
        else
        {
            var todo = view.SelectedTodo;
            lines.Add(new Markup($"[bold]{Markup.Escape(todo.Title)}[/]"));
            lines.Add(new Text($"Project: {view.SelectedProjectTitle}"));

            if (!string.IsNullOrEmpty(todo.SectionPath))
            {
                lines.Add(new Text($"Section: {todo.SectionPath}"));
            }

            AddField(lines, "Reference", todo.ExternalReference);
            AddField(lines, "Priority", todo.Priority?.ToString());
            AddField(lines, "Tags", todo.Tags.Length == 0 ? null : string.Join(", ", todo.Tags.Select(tag => $"#{tag}")));
            AddField(lines, "Start", todo.StartDate?.ToString("yyyy-MM-dd"));
            AddField(lines, "Due", todo.DueDate?.ToString("yyyy-MM-dd"));

            if (todo.Notes.Length == 0 && todo.Subtasks.Length == 0)
            {
                lines.Add(new Text(string.Empty));
                lines.Add(new Text("No additional details"));
            }
            else
            {
                if (todo.Notes.Length > 0)
                {
                    lines.Add(new Text(string.Empty));
                    lines.Add(new Markup("[bold]Notes[/]"));
                    lines.AddRange(todo.Notes.Select(note => new Text($"• {note}")));
                }

                if (todo.Subtasks.Length > 0)
                {
                    lines.Add(new Text(string.Empty));
                    lines.Add(new Markup("[bold]Subtasks[/]"));
                    lines.AddRange(todo.Subtasks.Select(subtask => new Text(FormatTodo(subtask, 0, false))));
                }
            }
        }

        return CreateContent(lines);
    }

    private static IRenderable CreateContent(IEnumerable<IRenderable> lines)
    {
        var content = lines.ToArray();
        return content.Length == 0 ? new Text(string.Empty) : new Rows(content);
    }

    private static string FormatTodo(TodoRow row) => FormatTodo(row.Todo!, row.Depth, row.IsSelected);

    private static string FormatTodo(TodoItem todo, int depth, bool selected)
    {
        var cursor = selected ? ">" : " ";
        var indent = new string(' ', depth * 2);
        var status = todo.IsCompleted ? "[x]" : "[ ]";
        var reference = todo.ExternalReference is null ? string.Empty : $"{todo.ExternalReference} - ";
        var priority = PriorityMarker(todo.Priority);
        var tags = todo.Tags.Length == 0 ? string.Empty : $" {string.Join(' ', todo.Tags.Select(tag => $"#{tag}"))}";
        var start = todo.StartDate is null ? string.Empty : $" 🛫 {todo.StartDate:yyyy-MM-dd}";
        var due = todo.DueDate is null ? string.Empty : $" 📅 {todo.DueDate:yyyy-MM-dd}";

        return $"{cursor} {indent}{status} {reference}{todo.Title}{priority}{tags}{start}{due}";
    }

    private static string PriorityMarker(TodoPriority? priority) => priority switch
    {
        TodoPriority.Highest => " 🔺",
        TodoPriority.High => " ⏫",
        TodoPriority.Medium => " 🔼",
        TodoPriority.Low => " 🔽",
        TodoPriority.Lowest => " ⏬",
        _ => string.Empty
    };

    private static void AddField(List<IRenderable> lines, string name, string? value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            lines.Add(new Text($"{name}: {value}"));
        }
    }

    private static void WriteStatus(BrowserView view, bool compact)
    {
        var status = view.State.IsCommandMode
            ? view.State.Command
            : view.State.Error ?? (compact
                ? ": commands  Esc back"
                : "↑↓ navigate  Tab pane  Enter select  : command  :completed  :q");

        AnsiConsole.Write(new Panel(new Text(status))
        {
            Border = BoxBorder.Rounded,
            Expand = true
        });
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
