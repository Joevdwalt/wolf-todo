using Spectre.Console;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: WolfTodo.ComponentHarness <dialog|titleeditor>");
    return 2;
}

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    Environment.Exit(0);
};

return args[0].ToLowerInvariant() switch
{
    "dialog" => RunDialog(),
    "titleeditor" => RunTitleEditor(),
    _ => InvalidScenario()
};

static int RunDialog()
{
    var bindings = TuiKeyBindings.CreateDefaults(":q");
    var projects = new[] { new TodoEditorProjectOption("Client work", "/fixtures/client-work.md") };
    var reducer = new TodoEditorReducer(() => new DateOnly(2026, 7, 27));
    var editor = DialogFixture.Create();
    var message = "Sandbox only — Markdown files are never changed.";

    while (true)
    {
        AnsiConsole.Clear();
        var view = TodoTaskEditorDialog.Create(editor, bindings, Console.WindowWidth, Console.WindowHeight);
        var content = new Rows(
            new Text("WOLF TODO COMPONENTS // TASK EDIT DIALOG", new Style(TuiThemes.Wolf.Heading, decoration: Decoration.Bold)),
            new Text(message, new Style(TuiThemes.Wolf.Muted, decoration: Decoration.Dim)),
            new Text(string.Empty),
            TodoTaskEditorDialog.CreateRenderable(view, TuiThemes.Wolf));
        AnsiConsole.Write(new Align(content, HorizontalAlignment.Center, VerticalAlignment.Middle));

        var key = Console.ReadKey(intercept: true);
        var transition = reducer.Reduce(editor, key, bindings, projects);
        if (transition.State is null)
        {
            return 0;
        }

        if (transition.Operation is TodoEditorOperation.Create or TodoEditorOperation.Update)
        {
            editor = DialogFixture.Create();
            message = $"Sandbox captured {transition.Operation.ToString().ToLowerInvariant()}; fixture reset.";
            continue;
        }

        editor = transition.State;
        message = "Sandbox only — Markdown files are never changed.";
    }
}

static int RunTitleEditor()
{
    var state = TodoTitleEditor.Create("Prepare customer workshop");
    var message = "Sandbox only — accepted titles are kept in memory.";

    while (true)
    {
        AnsiConsole.Clear();
        var content = new Rows(
            new Text("WOLF TODO COMPONENTS // TASK TITLE EDITOR", new Style(TuiThemes.Wolf.Heading, decoration: Decoration.Bold)),
            new Text(message, new Style(TuiThemes.Wolf.Muted, decoration: Decoration.Dim)),
            new Text(string.Empty),
            new Panel(new Rows(
                TodoTitleEditor.CreateRenderable(state, TuiThemes.Wolf),
                new Text("Enter ACCEPT  Esc CANCEL", new Style(TuiThemes.Wolf.Muted, decoration: Decoration.Dim))))
            {
                Header = new PanelHeader("EDIT TASK TITLE"),
                Border = BoxBorder.Square,
                BorderStyle = new Style(TuiThemes.Wolf.BorderActive),
                Expand = true
            });
        AnsiConsole.Write(new Align(content, HorizontalAlignment.Center, VerticalAlignment.Middle));

        var transition = TodoTitleEditor.Reduce(state, Console.ReadKey(intercept: true));
        if (transition.Outcome == TodoTitleEditorOutcome.Cancelled)
        {
            return 0;
        }

        if (transition.Outcome == TodoTitleEditorOutcome.Accepted)
        {
            message = $"Sandbox captured '{transition.State!.Text.Trim()}'; fixture reset.";
            state = TodoTitleEditor.Create("Prepare customer workshop");
            continue;
        }

        state = transition.State!;
        message = "Sandbox only — accepted titles are kept in memory.";
    }
}

static int InvalidScenario()
{
    Console.Error.WriteLine("Usage: WolfTodo.ComponentHarness <dialog|titleeditor>");
    return 2;
}

internal static class DialogFixture
{
    public static TodoTaskEditorState Create()
    {
        var todo = new TodoItem(
            12,
            false,
            "ACME-42",
            "Prepare customer workshop",
            TodoPriority.High,
            ["client", "workshop"],
            null,
            null,
            "Delivery",
            [new TodoNote(13, "Confirm the attendee list and room setup.")],
            [new TodoItem(14, false, null, "Draft the agenda", null, [], null, null, string.Empty, [], []),
             new TodoItem(15, true, null, "Send pre-read material", null, [], null, null, string.Empty, [], [])])
        {
            Schedule = new TodoSchedule(new DateOnly(2026, 7, 30), new TimeOnly(10, 30)),
            Duration = TimeSpan.FromMinutes(90)
        };

        return TodoTaskEditorState.Edit(todo, new TodoIdentity("/fixtures/client-work.md", todo.SourceLine)) with
        {
            SelectedIndex = (int)TodoFormField.Tags
        };
    }
}
