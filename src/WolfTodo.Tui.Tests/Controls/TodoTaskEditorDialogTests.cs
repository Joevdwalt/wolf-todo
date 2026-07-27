using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Tests.Controls;

public sealed class TodoTaskEditorDialogTests
{
    private static readonly TuiKeyBindings Bindings = TuiKeyBindings.CreateDefaults(":q");

    [Fact]
    public void Create_renders_the_rich_editor_fixture_with_selected_field_and_content()
    {
        var view = TodoTaskEditorDialog.Create(Editor(), Bindings, 100, 30);

        view.Lines.Select(line => line.Text).Should().Contain(line => line.Contains("EDIT TASK // Prepare workshop"))
            .And.Contain(line => line.Contains("> TAGS", StringComparison.Ordinal))
            .And.Contain(line => line.Contains("#client #workshop", StringComparison.Ordinal))
            .And.Contain(line => line.Contains("Confirm attendees", StringComparison.Ordinal))
            .And.Contain(line => line.Contains("Draft the agenda", StringComparison.Ordinal));
        view.Lines.Single(line => line.Text.Contains("> TAGS", StringComparison.Ordinal)).Role
            .Should().Be(TodoTaskEditorDialogRole.ActiveValue);
    }

    [Fact]
    public void Create_renders_a_validation_error_with_error_role()
    {
        var view = TodoTaskEditorDialog.Create(Editor() with { Error = "Schedule time is required." }, Bindings, 80, 24);

        view.Lines.Should().Contain(line =>
            line.Text.Contains("Schedule time is required.", StringComparison.Ordinal) &&
            line.Role == TodoTaskEditorDialogRole.Error);
    }

    [Fact]
    public void Create_renders_content_type_and_removal_modes()
    {
        var picker = TodoTaskEditorDialog.Create(
            Editor() with { Mode = TodoTaskEditorMode.ChooseContentType }, Bindings, 80, 24);
        var confirmation = TodoTaskEditorDialog.Create(
            Editor() with
            {
                SelectedIndex = TodoTaskEditorState.FieldCount + 1,
                Mode = TodoTaskEditorMode.ConfirmRemoval
            }, Bindings, 80, 24);

        picker.Lines.Should().Contain(line => line.Text.Contains("SELECT", StringComparison.Ordinal));
        confirmation.Lines.Should().Contain(line =>
            line.Text.Contains("REMOVE 'Draft the agenda'", StringComparison.Ordinal) &&
            line.Role == TodoTaskEditorDialogRole.Warning);
    }

    private static TodoTaskEditorState Editor()
    {
        var todo = new TodoItem(
            1, false, "ACME-42", "Prepare workshop", TodoPriority.High, ["client", "workshop"],
            null, null, "Delivery", [new TodoNote(2, "Confirm attendees")],
            [new TodoItem(3, false, null, "Draft the agenda", null, [], null, null, string.Empty, [], [])])
        {
            Schedule = new TodoSchedule(new DateOnly(2026, 7, 30), new TimeOnly(10, 30)),
            Duration = TimeSpan.FromMinutes(90)
        };
        return TodoTaskEditorState.Edit(todo, new TodoIdentity("/fixtures/work.md", 1)) with
        {
            SelectedIndex = (int)TodoFormField.Tags
        };
    }
}
