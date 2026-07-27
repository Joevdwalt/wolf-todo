using Spectre.Console;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;

if (args.Length != 1 || !string.Equals(args[0], "dialog", StringComparison.OrdinalIgnoreCase))
{
    Console.Error.WriteLine("Usage: WolfTodo.ComponentHarness dialog");
    return 2;
}

var bindings = TuiKeyBindings.CreateDefaults(":q");
var projects = new[] { new TodoEditorProjectOption("Client work", "/fixtures/client-work.md") };
var reducer = new TodoEditorReducer(() => new DateOnly(2026, 7, 27));
var editor = DialogFixture.Create();
var message = "Sandbox only — Markdown files are never changed.";

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    Environment.Exit(0);
};

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
        break;
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

return 0;

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
