using FluentAssertions;
using Spectre.Console;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Infrastructure;

namespace WolfTodo.Tui.Tests.Infrastructure;

public sealed class SpectreTerminalUiTests
{
    [Fact]
    public void ShowBrowser_renders_and_updates_the_selected_project_and_todo()
    {
        var todo = new TodoItem(
            1,
            false,
            "134416",
            "Milas Contract Renewal",
            TodoPriority.High,
            ["now"],
            new DateOnly(2026, 7, 8),
            null,
            "Renewals",
            ["Review current contract"],
            []);
        var state = BrowserState.Initial;
        var view = new BrowserView(
            state,
            [new ProjectRow("All", 1, null, null, true)],
            [new TodoRow(null, todo, 0, true)],
            todo,
            "All",
            "/todos/contracts.md",
            null,
            string.Empty);
        var terminal = new SpectreTerminalUi(() => 140, () => 30);
        StartRecording();

        terminal.ShowBrowser(view);
        terminal.ShowBrowser(view with { SelectedProjectTitle = "Personal" });
        var output = AnsiConsole.ExportText();

        output.Should().Contain("All").And.Contain("Personal").And.Contain("Milas Contract Renewal");
        output.Should().Contain("Projects").And.Contain("Todos: All").And.Contain("Details");
    }

    [Fact]
    public void ShowBrowser_keeps_wide_column_boundaries_when_details_wrap()
    {
        var shortView = ViewWithTitle("Short title");
        var longView = ViewWithTitle(new string('x', 160));

        var shortHeader = RenderHeader(shortView);
        var longHeader = RenderHeader(longView);

        longHeader.IndexOf("Todos:", StringComparison.Ordinal)
            .Should().Be(shortHeader.IndexOf("Todos:", StringComparison.Ordinal));
        longHeader.IndexOf("Details", StringComparison.Ordinal)
            .Should().Be(shortHeader.IndexOf("Details", StringComparison.Ordinal));
    }

    [Fact]
    public void ShowBrowser_truncates_todo_titles_and_keeps_metadata_in_details()
    {
        const string title = "Prepare the unusually detailed contract renewal proposal for the customer before the quarterly review meeting";
        var todo = new TodoItem(
            1,
            false,
            "134416",
            title,
            TodoPriority.High,
            ["now"],
            new DateOnly(2026, 7, 8),
            new DateOnly(2026, 7, 12),
            "Renewals",
            ["Review current contract"],
            []);
        var view = new BrowserView(
            BrowserState.Initial,
            [new ProjectRow("All", 1, null, null, true)],
            [new TodoRow(null, todo, 0, true)],
            todo,
            "All",
            "/todos/contracts.md",
            null,
            string.Empty);

        StartRecording();
        new SpectreTerminalUi(() => 140, () => 30).ShowBrowser(view);
        var output = AnsiConsole.ExportText();
        var todoLine = output.Split(Environment.NewLine)
            .Last(line => line.Contains("Prepare the unusually", StringComparison.Ordinal));
        var todoPane = todoLine.Split('│')[2];

        todoPane.Should().Contain("Prepare the unusually").And.Contain("…").And.Contain("⏫");
        todoPane.Should().NotContain("134416").And.NotContain("#now").And.NotContain("2026-07-08");
        output.Should().Contain("quarterly review")
            .And.Contain("meeting")
            .And.Contain("Reference: 134416")
            .And.Contain("Tags: #now")
            .And.Contain("Start: 2026-07-08")
            .And.Contain("Due: 2026-07-12");
    }

    [Fact]
    public void ShowBrowser_renders_filter_editing_and_committed_filter_statuses()
    {
        var terminal = new SpectreTerminalUi(() => 140, () => 30);
        var view = ViewWithTitle("Renew contract");
        StartRecording();

        terminal.ShowBrowser(view with
        {
            State = view.State with { IsFilterMode = true, FilterDraft = "renew" }
        });
        terminal.ShowBrowser(view with
        {
            State = view.State with { FilterText = "renew" }
        });
        var output = AnsiConsole.ExportText();

        output.Should().Contain("/renew");
        output.Should().Contain("Filter: /renew").And.Contain("empty Enter clears");
    }

    [Fact]
    public void ShowBrowser_includes_the_filter_key_in_wide_and_compact_hints()
    {
        var view = ViewWithTitle("Renew contract");
        StartRecording();

        new SpectreTerminalUi(() => 140, () => 30).ShowBrowser(view);
        new SpectreTerminalUi(() => 70, () => 16).ShowBrowser(view);
        var output = AnsiConsole.ExportText();

        output.Should().Contain("/ filter  : command");
        output.Should().Contain("/ filter  : commands  Esc back");
    }

    [Theory]
    [InlineData(140, 30, BrowserFocus.Projects)]
    [InlineData(100, 20, BrowserFocus.Todos)]
    [InlineData(70, 16, BrowserFocus.Todos)]
    public void ShowBrowser_keeps_the_status_position_when_filtering_reduces_results(
        int width,
        int height,
        BrowserFocus focus)
    {
        var unfiltered = RenderBrowser(ViewWithTodoCount(8, focus), width, height);
        var filteredView = ViewWithTodoCount(1, focus);
        var filtered = RenderBrowser(
            filteredView with { State = filteredView.State with { FilterText = "Todo 1" } },
            width,
            height);

        unfiltered.Length.Should().BeGreaterThanOrEqualTo(height);
        filtered.Length.Should().BeGreaterThanOrEqualTo(height);
        StatusPanelTop(unfiltered).Should().Be(StatusPanelTop(filtered));
    }

    [Fact]
    public void ShowBrowser_expands_beyond_the_minimum_height_without_cropping_content()
    {
        var lines = RenderBrowser(ViewWithTodoCount(25, BrowserFocus.Todos), 140, 24);

        lines.Length.Should().BeGreaterThan(24);
        lines.Should().Contain(line => line.Contains("Todo 25", StringComparison.Ordinal));
    }

    private static BrowserView ViewWithTitle(string title)
    {
        var todo = new TodoItem(1, false, null, title, null, [], null, null, string.Empty, [], []);
        return new BrowserView(
            BrowserState.Initial,
            [new ProjectRow("All", 1, null, null, true)],
            [new TodoRow(null, todo, 0, true)],
            todo,
            "All",
            "/todos/project.md",
            null,
            string.Empty);
    }

    private static BrowserView ViewWithTodoCount(int count, BrowserFocus focus)
    {
        var todos = Enumerable.Range(1, count)
            .Select(index => new TodoItem(
                index,
                false,
                null,
                $"Todo {index}",
                null,
                [],
                null,
                null,
                string.Empty,
                [],
                []))
            .ToArray();
        var rows = todos.Select((todo, index) => new TodoRow(null, todo, 0, index == 0)).ToArray();

        return new BrowserView(
            BrowserState.Initial with { Focus = focus },
            [new ProjectRow("All", count, null, null, true)],
            [.. rows],
            todos[0],
            "All",
            "/todos/project.md",
            null,
            string.Empty);
    }

    private static string[] RenderBrowser(BrowserView view, int width, int height)
    {
        StartRecording(width, height);
        var existingOutputLength = AnsiConsole.ExportText().Length;
        new SpectreTerminalUi(() => width, () => height).ShowBrowser(view);
        return AnsiConsole.ExportText()[existingOutputLength..]
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
    }

    private static int StatusPanelTop(string[] lines) =>
        Array.FindLastIndex(lines, line => line.StartsWith('╭'));

    private static string RenderHeader(BrowserView view)
    {
        StartRecording();
        new SpectreTerminalUi(() => 140, () => 30).ShowBrowser(view);
        return AnsiConsole.ExportText()
            .Split(Environment.NewLine)
            .First(line => line.Contains("Projects", StringComparison.Ordinal));
    }

    private static void StartRecording()
    {
        StartRecording(140, 30);
    }

    private static void StartRecording(int width, int height)
    {
        AnsiConsole.Record();
        AnsiConsole.Profile.Width = width;
        AnsiConsole.Profile.Height = height;
    }
}
