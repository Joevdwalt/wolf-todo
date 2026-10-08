using System.Collections.Immutable;
using FluentAssertions;
using Spectre.Console;
using Spectre.Console.Rendering;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ProjectBrowser.Rendering;

namespace WolfTodo.Tui.Tests.Features.ProjectBrowser.Rendering;

public sealed class TodoRowRendererTests
{
    private readonly TodoRowRenderer renderer = new();

    [Fact]
    public void FormatSchedule_includes_time_only_when_present()
    {
        renderer.FormatSchedule(new TodoSchedule(new DateOnly(2026, 8, 4), null))
            .Should().Be("2026-08-04");
        renderer.FormatSchedule(new TodoSchedule(new DateOnly(2026, 8, 4), new TimeOnly(9, 30)))
            .Should().Be("2026-08-04 09:30");
    }

    [Fact]
    public void FormatDuration_uses_minutes()
    {
        renderer.FormatDuration(TimeSpan.FromMinutes(45)).Should().Be("45m");
        renderer.FormatDuration(null).Should().BeNull();
    }

    [Fact]
    public void PriorityCode_maps_known_priorities_and_missing_priority()
    {
        renderer.PriorityCode(TodoPriority.Highest).Should().Be("!");
        renderer.PriorityCode(TodoPriority.High).Should().Be("H");
        renderer.PriorityCode(TodoPriority.Medium).Should().Be("M");
        renderer.PriorityCode(TodoPriority.Low).Should().Be("L");
        renderer.PriorityCode(TodoPriority.Lowest).Should().Be(".");
        renderer.PriorityCode(null).Should().Be("-");
    }

    [Fact]
    public void StatusGlyph_maps_completion_state()
    {
        renderer.StatusGlyph(false).Should().Be("◯");
        renderer.StatusGlyph(true).Should().Be("✓");
    }

    [Fact]
    public void Truncate_preserves_display_width_budget()
    {
        renderer.Truncate("abcdef", 4).Should().Be("abc…");
        renderer.Truncate("abc", 4).Should().Be("abc");
    }

    [Fact]
    public void FlattenSubtasks_returns_depth_first_rows_with_tree_paths()
    {
        var firstChild = Todo("First child");
        var lastChild = Todo("Last child");
        var firstRoot = Todo("First root", [firstChild, lastChild]);
        var lastRoot = Todo("Last root");
        var parentPath = ImmutableArray.Create(TodoTreeSegment.HasFollowingSibling);

        var flattened = renderer.FlattenSubtasks([firstRoot, lastRoot], parentPath).ToArray();

        flattened.Select(row => row.Todo.Title)
            .Should().Equal("First root", "First child", "Last child", "Last root");
        flattened[0].TreePath.Should().Equal(TodoTreeSegment.HasFollowingSibling, TodoTreeSegment.HasFollowingSibling);
        flattened[1].TreePath.Should().Equal(
            TodoTreeSegment.HasFollowingSibling,
            TodoTreeSegment.HasFollowingSibling,
            TodoTreeSegment.HasFollowingSibling);
        flattened[2].TreePath.Should().Equal(
            TodoTreeSegment.HasFollowingSibling,
            TodoTreeSegment.HasFollowingSibling,
            TodoTreeSegment.LastSibling);
        flattened[3].TreePath.Should().Equal(TodoTreeSegment.HasFollowingSibling, TodoTreeSegment.LastSibling);
    }

    [Fact]
    public void FlattenSubtasks_returns_no_rows_for_empty_input()
    {
        renderer.FlattenSubtasks([]).Should().BeEmpty();
    }

    [Fact]
    public void DetailLine_renders_selected_task_details()
    {
        var todo = new TodoItem(
            3,
            false,
            "REF-1",
            "Plan [draft]",
            TodoPriority.High,
            ["home", "urgent"],
            null,
            null,
            "Work",
            [],
            [])
        {
            Schedule = new TodoSchedule(new DateOnly(2026, 8, 4), new TimeOnly(9, 30))
        };

        var output = Render(renderer.DetailLine(
            todo,
            [TodoTreeSegment.LastSibling],
            selected: true,
            TuiThemes.Wolf));

        output.Should().Be("> └─ ◯ H REF-1 - Plan [draft] #home #urgent ⏳ 2026-08-04 09:30");
    }

    [Fact]
    public void DetailLine_omits_empty_optional_details_for_completed_task()
    {
        var todo = new TodoItem(3, true, null, "Done", null, [], null, null, string.Empty, [], []);

        var output = Render(renderer.DetailLine(todo, [], selected: false, TuiThemes.Wolf));

        output.Should().Be("  ✓ - Done");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void AppendDetailSegment_uses_task_completion_style(bool isCompleted)
    {
        var line = new System.Text.StringBuilder();
        var theme = TuiThemes.Wolf;

        TodoRowRenderer.AppendDetailSegment(line, "#home", theme.Tag, isCompleted, theme);

        var expectedStyle = isCompleted
            ? $"[{theme.Muted.ToMarkup()} dim]"
            : $"[{theme.Tag.ToMarkup()}]";
        line.ToString().Should().Be($"{expectedStyle}#home[/]");
    }

    private static string Render(IRenderable renderable)
    {
        var writer = new StringWriter();
        var console = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.No,
            Out = new AnsiConsoleOutput(writer)
        });
        console.Profile.Width = 120;
        console.Write(renderable);
        return writer.ToString().TrimEnd();
    }

    private static TodoItem Todo(string title, ImmutableArray<TodoItem> subtasks = default) =>
        new(
            1,
            false,
            null,
            title,
            null,
            [],
            null,
            null,
            string.Empty,
            [],
            subtasks.IsDefault ? ImmutableArray<TodoItem>.Empty : subtasks);
}
