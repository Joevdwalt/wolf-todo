using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Splash;

namespace WolfTodo.Tui.Infrastructure;

public sealed class SpectreTerminalUi : ITerminalUi
{
    private bool browserRendered;

    public void ShowSplash(string logo)
    {
        browserRendered = false;
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

        var width = SafeWindowWidth();
        var height = SafeWindowHeight();

        if (width >= 120 && height >= 24)
        {
            WriteWide(view);
        }
        else if (width >= 80 && height >= 18)
        {
            WriteMedium(view);
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

    private static void WriteWide(BrowserView view)
    {
        var table = CreateLayoutTable(3);
        table.AddRow(
            ProjectPanel(view),
            TodoPanel(view),
            DetailPanel(view));
        AnsiConsole.Write(table);
    }

    private static void WriteMedium(BrowserView view)
    {
        var table = CreateLayoutTable(2);
        table.AddRow(
            ProjectPanel(view),
            view.State.Focus == BrowserFocus.Details ? DetailPanel(view) : TodoPanel(view));
        AnsiConsole.Write(table);
    }

    private static void WriteNarrow(BrowserView view)
    {
        var panel = view.State.Focus switch
        {
            BrowserFocus.Projects => ProjectPanel(view),
            BrowserFocus.Todos => TodoPanel(view),
            _ => DetailPanel(view)
        };

        AnsiConsole.Write(panel);
    }

    private static Table CreateLayoutTable(int columns)
    {
        var table = new Table().NoBorder().Expand();
        table.HideHeaders();

        for (var index = 0; index < columns; index++)
        {
            table.AddColumn(new TableColumn(string.Empty));
        }

        return table;
    }

    private static Panel ProjectPanel(BrowserView view)
    {
        var lines = view.Projects.Select(row =>
        {
            var cursor = row.IsSelected ? ">" : " ";
            var error = row.Error is null ? " " : "!";
            var count = row.Error is null ? $" {row.ActiveCount}" : string.Empty;
            return new Text($"{cursor}{error} {row.Title}{count}");
        });

        return CreatePanel("Projects", lines, view.State.Focus == BrowserFocus.Projects);
    }

    private static Panel TodoPanel(BrowserView view)
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
                : new Text(FormatTodo(row)));
        }

        return CreatePanel(
            $"Todos: {view.SelectedProjectTitle}",
            lines,
            view.State.Focus == BrowserFocus.Todos);
    }

    private static Panel DetailPanel(BrowserView view)
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

        return CreatePanel("Details", lines, view.State.Focus == BrowserFocus.Details);
    }

    private static Panel CreatePanel(string title, IEnumerable<IRenderable> lines, bool focused)
    {
        var content = lines.ToArray();
        var panel = new Panel(content.Length == 0 ? new Text(string.Empty) : new Rows(content))
        {
            Header = new PanelHeader(title),
            Expand = true,
            Border = BoxBorder.Rounded,
            BorderStyle = focused ? new Style(Color.Cyan1) : Style.Plain
        };

        return panel;
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
