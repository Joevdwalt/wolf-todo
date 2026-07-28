using Spectre.Console;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;

if (args.Length != 1)
{
    Console.Error.WriteLine("Usage: WolfTodo.ComponentHarness <dialog|textbox:edit|textbox:readonly|multiline|select-list>");
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
    "textbox" or "titleeditor" or "textbox:edit" => RunTextBox(editable: true),
    "textbox:readonly" => RunTextBox(editable: false),
    "multiline" => RunMultilineTextBox(),
    "select-list" => RunSelectList(),
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

static int RunTextBox(bool editable)
{
    var state = TextBox.Create("Title", editable, "Prepare customer workshop", isActive: editable);
    var message = editable
        ? "Sandbox only — accepted titles are kept in memory."
        : "Read-only fixture — text input is ignored.";

    while (true)
    {
        AnsiConsole.Clear();
        var content = new Rows(
            new Text(
                $"WOLF TODO COMPONENTS // TEXT BOX // {(editable ? "EDIT" : "READONLY")}",
                new Style(TuiThemes.Wolf.Heading, decoration: Decoration.Bold)),
            new Text(message, new Style(TuiThemes.Wolf.Muted, decoration: Decoration.Dim)),
            new Text(string.Empty),
            TextBox.CreateRenderable(state, TuiThemes.Wolf, Math.Max(20, 20)),
            new Text(editable ? "Enter ACCEPT  Esc CANCEL" : "Esc CLOSE", new Style(TuiThemes.Wolf.Muted, decoration: Decoration.Dim)));
        AnsiConsole.Write(new Align(content, HorizontalAlignment.Center, VerticalAlignment.Middle));

        var key = Console.ReadKey(intercept: true);
        if (!editable && key.Key == ConsoleKey.Escape)
        {
            return 0;
        }

        var transition = TextBox.Default.Reduce(state, key, TuiKeyBindings.CreateDefaults(":q"));
        if (transition.Outcome == TextBoxOutcome.Cancelled)
        {
            return 0;
        }

        if (transition.Outcome == TextBoxOutcome.Accepted)
        {
            message = $"Sandbox captured '{transition.State!.Text.Trim()}'; fixture reset.";
            state = TextBox.Create("Title", editable, "Prepare customer workshop", isActive: editable);
            continue;
        }

        state = transition.State!;
        message = editable
            ? "Sandbox only — accepted titles are kept in memory."
            : "Read-only fixture — text input is ignored.";
    }
}

static int RunMultilineTextBox()
{
    var bindings = TuiKeyBindings.CreateDefaults(":q");
    var state = MultilineTextBoxState.Create(
        "Notes",
        "Confirm attendees\nBook the meeting room",
        isMultiline: true);
    var message = "Sandbox only — accepted text is kept in memory.";

    while (true)
    {
        AnsiConsole.Clear();
        var content = new Rows(
            new Text("WOLF TODO COMPONENTS // MULTILINE TEXT BOX", new Style(TuiThemes.Wolf.Heading, decoration: Decoration.Bold)),
            new Text(message, new Style(TuiThemes.Wolf.Muted, decoration: Decoration.Dim)),
            new Text(string.Empty),
            MultilineTextBox.Default.Render(
                state,
                TuiThemes.Wolf,
                new TuiComponentConstraints(Math.Max(20, Console.WindowWidth - 8), 6),
                TuiKeyBindings.ShortestDisplayName(bindings.SaveForm)));
        AnsiConsole.Write(new Align(content, HorizontalAlignment.Center, VerticalAlignment.Middle));

        var transition = MultilineTextBox.Default.Reduce(state, Console.ReadKey(intercept: true), bindings);
        if (transition.Outcome == MultilineTextBoxOutcome.Cancelled)
        {
            return 0;
        }

        if (transition.Outcome == MultilineTextBoxOutcome.Accepted)
        {
            message = $"Sandbox captured {transition.State!.Text.Length} character(s); fixture reset.";
            state = MultilineTextBoxState.Create("Notes", "Confirm attendees\nBook the meeting room", true);
            continue;
        }

        state = transition.State!;
        message = "Sandbox only — accepted text is kept in memory.";
    }
}

static int RunSelectList()
{
    var bindings = TuiKeyBindings.CreateDefaults(":q");
    var state = new SelectListView(
        "Choose priority",
        [new SelectOption("Highest"), new SelectOption("High"), new SelectOption("Medium"), new SelectOption("Low")],
        1,
        null,
        "No priorities available.",
        $"{TuiKeyBindings.ShortestDisplayName(bindings.MoveDown)}/{TuiKeyBindings.ShortestDisplayName(bindings.MoveUp)} MOVE  " +
        $"{TuiKeyBindings.ShortestDisplayName(bindings.Open)} SELECT  {TuiKeyBindings.ShortestDisplayName(bindings.Back)} CANCEL");
    var message = "Sandbox only — selections are kept in memory.";

    while (true)
    {
        AnsiConsole.Clear();
        var content = new Rows(
            new Text("WOLF TODO COMPONENTS // SELECT LIST", new Style(TuiThemes.Wolf.Heading, decoration: Decoration.Bold)),
            new Text(message, new Style(TuiThemes.Wolf.Muted, decoration: Decoration.Dim)),
            new Text(string.Empty),
            SelectList.Default.Render(state, TuiThemes.Wolf, new TuiComponentConstraints(Math.Max(20, Console.WindowWidth - 8), 5)));
        AnsiConsole.Write(new Align(content, HorizontalAlignment.Center, VerticalAlignment.Middle));

        var transition = SelectList.Default.Reduce(state, Console.ReadKey(intercept: true), bindings);
        if (transition.Outcome == SelectListOutcome.Cancelled)
        {
            return 0;
        }

        if (transition.Outcome == SelectListOutcome.Accepted)
        {
            message = $"Sandbox selected '{transition.State!.Options[transition.State.ClampedSelectedIndex].Label}'.";
            continue;
        }

        state = transition.State!;
        message = "Sandbox only — selections are kept in memory.";
    }
}

static int InvalidScenario()
{
    Console.Error.WriteLine("Usage: WolfTodo.ComponentHarness <dialog|textbox:edit|textbox:readonly|multiline|select-list>");
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
