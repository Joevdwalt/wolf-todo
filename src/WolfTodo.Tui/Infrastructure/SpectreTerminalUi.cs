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
            WriteWide(view, width, height);
        }
        else if (width >= 80 && height >= 18)
        {
            WriteMedium(view, width, height);
        }
        else
        {
            WriteNarrow(view, width, height);
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

    private static void WriteWide(BrowserView view, int terminalWidth, int terminalHeight)
    {
        const int projectWidth = 22;
        const int frameAndPaddingWidth = 10;
        var remainingWidth = terminalWidth - projectWidth - frameAndPaddingWidth;
        var todoWidth = remainingWidth / 2;
        var detailWidth = remainingWidth - todoWidth;
        var projectLines = ProjectLines(view);
        var todoLines = TodoLines(view, todoWidth - 2);
        var detailLines = DetailLines(view);
        var table = CreatePaneTable(
            ("Projects", projectWidth, view.State.Focus == BrowserFocus.Projects, true),
            ($"Todos: {view.SelectedProjectTitle}", todoWidth, view.State.Focus == BrowserFocus.Todos, true),
            ("Details", detailWidth, view.State.Focus == BrowserFocus.Details, false));
        table.AddRow(
            CreateContent(projectLines),
            CreateContent(todoLines),
            CreateContent(detailLines));
        PadToMinimumHeight(table, terminalHeight, projectLines.Count, todoLines.Count, detailLines.Count);
        AnsiConsole.Write(table);
    }

    private static void WriteMedium(BrowserView view, int terminalWidth, int terminalHeight)
    {
        const int projectWidth = 22;
        const int frameAndPaddingWidth = 7;
        var contentWidth = terminalWidth - projectWidth - frameAndPaddingWidth;
        var showDetails = view.State.Focus == BrowserFocus.Details;
        var projectLines = ProjectLines(view);
        var contentLines = showDetails ? DetailLines(view) : TodoLines(view, contentWidth - 2);
        var table = CreatePaneTable(
            ("Projects", projectWidth, view.State.Focus == BrowserFocus.Projects, true),
            (showDetails ? "Details" : $"Todos: {view.SelectedProjectTitle}", contentWidth, true, !showDetails));
        table.AddRow(
            CreateContent(projectLines),
            CreateContent(contentLines));
        PadToMinimumHeight(table, terminalHeight, projectLines.Count, contentLines.Count);
        AnsiConsole.Write(table);
    }

    private static void WriteNarrow(BrowserView view, int terminalWidth, int terminalHeight)
    {
        const int frameAndPaddingWidth = 4;
        var contentWidth = terminalWidth - frameAndPaddingWidth;
        var title = view.State.Focus switch
        {
            BrowserFocus.Projects => "Projects",
            BrowserFocus.Todos => $"Todos: {view.SelectedProjectTitle}",
            _ => "Details"
        };
        var lines = view.State.Focus switch
        {
            BrowserFocus.Projects => ProjectLines(view),
            BrowserFocus.Todos => TodoLines(view, contentWidth),
            _ => DetailLines(view)
        };
        var table = CreatePaneTable((title, null, true, view.State.Focus != BrowserFocus.Details));
        table.AddRow(CreateContent(lines));
        PadToMinimumHeight(table, terminalHeight, lines.Count);

        AnsiConsole.Write(table);
    }

    private static Table CreatePaneTable(params (string Title, int? Width, bool Focused, bool NoWrap)[] panes)
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
                NoWrap = pane.NoWrap,
                Padding = new Padding(1, 0)
            });
        }

        return table;
    }

    private static IReadOnlyList<IRenderable> ProjectLines(BrowserView view)
    {
        return view.Projects.Select(row =>
        {
            var cursor = row.IsSelected ? ">" : " ";
            var error = row.Error is null ? " " : "!";
            var count = row.Error is null ? $" {row.ActiveCount}" : string.Empty;
            return (IRenderable)new Text($"{cursor}{error} {row.Title}{count}").Ellipsis();
        }).ToArray();
    }

    private static IReadOnlyList<IRenderable> TodoLines(BrowserView view, int contentWidth)
    {
        if (view.Diagnostic is not null)
        {
            return [new Text("Select the error entry for details.")];
        }

        if (view.Todos.Length == 0)
        {
            return [new Text(view.EmptyMessage)];
        }

        return view.Todos.Select(row => row.Heading is not null
            ? (IRenderable)new Markup($"[bold]{Markup.Escape(row.Heading)}[/]").Ellipsis()
            : TodoListRow(row, contentWidth)).ToArray();
    }

    private static IReadOnlyList<IRenderable> DetailLines(BrowserView view)
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
                    lines.AddRange(todo.Subtasks.Select(subtask => new Text(FormatDetailedTodo(subtask, 0, false))));
                }
            }
        }

        return lines;
    }

    private static IRenderable CreateContent(IReadOnlyList<IRenderable> lines)
    {
        return lines.Count == 0 ? new Text(string.Empty) : new Rows(lines);
    }

    private static void PadToMinimumHeight(Table table, int terminalHeight, params int[] paneLineCounts)
    {
        const int tableAndStatusHeight = 6;
        var minimumContentHeight = Math.Max(1, terminalHeight - tableAndStatusHeight);
        var renderedContentHeight = Math.Max(1, paneLineCounts.Max());

        for (var row = renderedContentHeight; row < minimumContentHeight; row++)
        {
            table.AddEmptyRow();
        }
    }

    private static IRenderable TodoListRow(TodoRow row, int contentWidth)
    {
        const int priorityWidth = 3;
        var todo = row.Todo!;
        var cursor = row.IsSelected ? ">" : " ";
        var indent = new string(' ', row.Depth * 2);
        var status = todo.IsCompleted ? "[x]" : "[ ]";
        var prefix = $"{cursor} {indent}{status} ";
        var leftWidth = Math.Max(1, contentWidth - priorityWidth);
        var titleWidth = Math.Max(1, leftWidth - DisplayWidth(prefix));
        var title = Truncate(todo.Title, titleWidth);
        var left = prefix + title;
        var padding = new string(' ', Math.Max(0, leftWidth - DisplayWidth(left)));
        var priority = PriorityMarker(todo.Priority).Trim();

        return new Text($"{left}{padding}{priority,3}");
    }

    private static string Truncate(string value, int width)
    {
        if (DisplayWidth(value) <= width)
        {
            return value;
        }

        var result = new System.Text.StringBuilder();
        var remainingWidth = Math.Max(0, width - 1);

        foreach (var rune in value.EnumerateRunes())
        {
            var runeWidth = rune.ToString().GetCellWidth();
            if (runeWidth > remainingWidth)
            {
                break;
            }

            result.Append(rune.ToString());
            remainingWidth -= runeWidth;
        }

        return result.Append('…').ToString();
    }

    private static int DisplayWidth(string value) => value.GetCellWidth();

    private static string FormatDetailedTodo(TodoItem todo, int depth, bool selected)
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
        var status = view.State switch
        {
            { IsCommandMode: true } => view.State.Command,
            { IsFilterMode: true } => $"/{view.State.FilterDraft}",
            { Error: not null } => view.State.Error,
            { FilterText.Length: > 0 } => $"Filter: /{view.State.FilterText}  / edit  empty Enter clears",
            _ when compact => "/ filter  : commands  Esc back",
            _ => "↑↓ navigate  Tab pane  Enter select  / filter  : command  :completed  :q"
        };

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
