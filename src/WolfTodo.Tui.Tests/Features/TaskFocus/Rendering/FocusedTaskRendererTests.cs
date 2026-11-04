using System.Collections.Immutable;
using FluentAssertions;
using Spectre.Console;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.TaskFocus;
using WolfTodo.Tui.Features.TaskFocus.Rendering;

namespace WolfTodo.Tui.Tests.Features.TaskFocus.Rendering;

[Collection("Planner frames")]
public sealed class FocusedTaskRendererTests
{
    private static readonly TuiKeyBindings Bindings = TuiKeyBindings.CreateDefaults(":q");

    [Fact]
    public void Card_connects_siblings_and_nested_branches_with_rounded_final_children()
    {
        var output = RenderCard(View(Tree()));

        output.Should().Contain("> ◯ Root")
            .And.Contain("├─ ◯ First")
            .And.Contain("│  ├─ ◯ Earlier")
            .And.Contain("│  │  ╰─ ◯ Leaf")
            .And.Contain("│  ╰─ ◯ Final")
            .And.Contain("╰─ ◯ Last")
            .And.Contain("   ╰─ ◯ End")
            .And.NotContain("└─")
            .And.NotContain("●");
    }

    [Theory]
    [InlineData(2, "│   LINK: ")]
    [InlineData(3, "│  │   LINK: ")]
    [InlineData(5, "│      LINK: ")]
    [InlineData(7, "       LINK: ")]
    public void Card_continues_ancestor_and_sibling_guides_through_the_selected_link(int sourceLine, string prefix)
    {
        var view = View(Tree(), sourceLine);
        var output = RenderCard(view);

        output.Should().Contain(prefix + TaskLinkCode.Generate("/work.md", sourceLine));
        output.Should().Contain("● " + view.SelectedItem.Todo.Title);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Card_keeps_completion_symbols_and_selection_styling(bool completed)
    {
        var root = Todo(1, "Root", subtasks:
        [
            Todo(2, "Selected") with { IsCompleted = completed },
            Todo(3, "Open"),
            Todo(4, "Done") with { IsCompleted = true }
        ]);
        var view = View(root, 2);
        var theme = TuiThemes.Wolf with { AccentBright = new Color(1, 2, 3) };
        var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings { Out = new AnsiConsoleOutput(writer) });
        console.Profile.Width = 100;
        var recorder = console.CreateRecorder();

        recorder.Write(new FocusedTaskRenderer(() => 100, () => 30).Card(view, theme, 100, 20));

        recorder.ExportText().Should().Contain($"├─ {(completed ? "✓" : "●")} Selected")
            .And.Contain("├─ ◯ Open")
            .And.Contain("╰─ ✓ Done")
            .And.NotContain("> ●");
        recorder.ExportHtml().Should().Contain("#010203").And.Contain("bold");
    }

    [Fact]
    public void Moving_selection_updates_the_dot_and_link()
    {
        var root = Todo(1, "Root", subtasks: [Todo(2, "First"), Todo(3, "Last")]);
        var view = View(root, 2);
        var before = RenderCard(view);
        var transition = new FocusedTaskReducer().Reduce(view.State,
            new ConsoleKeyInfo('j', ConsoleKey.J, false, false, false), Bindings, view);
        var moved = new FocusedTaskPresenter().CreateView(Catalog(root), transition.State)!;
        var after = RenderCard(moved);

        before.Should().Contain("├─ ● First").And.Contain("╰─ ◯ Last")
            .And.Contain("LINK: " + TaskLinkCode.Generate("/work.md", 2));
        after.Should().Contain("├─ ◯ First").And.Contain("╰─ ● Last")
            .And.Contain("LINK: " + TaskLinkCode.Generate("/work.md", 3))
            .And.NotContain("LINK: " + TaskLinkCode.Generate("/work.md", 2));
    }

    [Theory]
    [InlineData(80, 15, false)]
    [InlineData(40, 15, false)]
    [InlineData(32, 16, false)]
    [InlineData(40, 15, true)]
    [InlineData(32, 16, true)]
    public void Short_and_narrow_frames_keep_the_selected_item_and_link_visible(int width, int height, bool nested)
    {
        var root = Todo(1, "Root", subtasks: Enumerable.Range(2, 20)
            .Select(index => Todo(index, $"Item {index}")).ToImmutableArray());
        var view = nested ? View(Tree(), 7) : View(root, 21);
        var previous = AnsiConsole.Console;
        var writer = new StringWriter();
        try
        {
            var console = AnsiConsole.Create(new AnsiConsoleSettings
            {
                Ansi = AnsiSupport.No, Out = new AnsiConsoleOutput(writer)
            });
            console.Profile.Width = width;
            console.Profile.Height = height;
            var recorder = console.CreateRecorder();
            AnsiConsole.Console = recorder;

            new FocusedTaskRenderer(() => width, () => height).ShowFocusedTask(view, Bindings, TuiThemes.Wolf);

            var output = recorder.ExportText();
            var lines = output.TrimEnd('\r', '\n').Split(Environment.NewLine);
            output.Should().Contain("╰─ ● " + view.SelectedItem.Todo.Title)
                .And.Contain("LINK: " + TaskLinkCode.Generate("/work.md", view.SelectedItem.Todo.SourceLine));
            // Adjacent full-width renderables rely on the terminal's automatic wrap.
            var physicalRows = lines.Sum(line => Math.Max(1, (line.GetCellWidth() + width - 1) / width));
            physicalRows.Should().BeLessThanOrEqualTo(height);
        }
        finally
        {
            AnsiConsole.Console = previous;
        }
    }

    private static TodoItem Tree() => Todo(1, "Root", subtasks:
    [
        Todo(2, "First", subtasks:
        [
            Todo(3, "Earlier", subtasks: [Todo(4, "Leaf")]), Todo(5, "Final")
        ]),
        Todo(6, "Last", subtasks: [Todo(7, "End")])
    ]);

    private static TodoItem Todo(int line, string title, ImmutableArray<TodoItem> subtasks = default) =>
        new(line, false, null, title, null, [], null, null, string.Empty, [], subtasks.IsDefault ? [] : subtasks);

    private static ProjectCatalog Catalog(TodoItem root) => new([new TodoProject("Work", "/work.md", [root])], []);

    private static FocusedTaskView View(TodoItem root, int selectedLine = 1) =>
        new FocusedTaskPresenter().CreateView(Catalog(root),
            FocusedTaskState.Create(new TodoIdentity("/work.md", 1), root) with
            {
                SelectedIdentity = new TodoIdentity("/work.md", selectedLine)
            })!;

    private static string RenderCard(FocusedTaskView view)
    {
        var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No, Out = new AnsiConsoleOutput(writer)
        });
        console.Profile.Width = 100;
        console.Write(new FocusedTaskRenderer(() => 100, () => 30).Card(view, TuiThemes.Wolf, 100, 20));
        return writer.ToString();
    }
}
