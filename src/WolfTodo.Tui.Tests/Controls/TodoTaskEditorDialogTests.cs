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
            .And.Contain(line => line.Contains("Confirm attendees", StringComparison.Ordinal))
            .And.Contain(line => line.Contains("Draft the agenda", StringComparison.Ordinal));
        var tags = view.TextBoxes!.Single(textBox => textBox.Label == "Tags");
        tags.Text.Should().Be("#client #workshop");
        tags.IsActive.Should().BeTrue();
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
    public void Create_renders_content_and_subtask_sections_and_removal_mode()
    {
        var confirmation = TodoTaskEditorDialog.Create(
            Editor() with
            {
                SelectedIndex = TodoTaskEditorState.ContentIndex + 1,
                Mode = TodoTaskEditorMode.ConfirmRemoval
            }, Bindings, 80, 24);

        var form = TodoTaskEditorDialog.Create(Editor(), Bindings, 80, 40);
        form.Lines.Should().Contain(line => line.Text.Contains("CONTENT", StringComparison.Ordinal));
        form.Lines.Should().Contain(line => line.Text.Contains("SUBTASKS", StringComparison.Ordinal));
        confirmation.Lines.Should().Contain(line =>
            line.Text.Contains("REMOVE 'Draft the agenda'", StringComparison.Ordinal) &&
            line.Role == TodoTaskEditorDialogRole.Warning);
    }

    [Fact]
    public void Create_embeds_an_editable_title_textbox_when_the_title_is_being_edited()
    {
        var reducer = new TodoEditorReducer();
        var editor = Editor() with { SelectedIndex = (int)TodoFormField.Title };
        var editing = reducer.Reduce(editor, Key('e'), Bindings, []).State!;

        var view = TodoTaskEditorDialog.Create(editing, Bindings, 80, 24);

        var title = view.TextBoxes!.Single(textBox => textBox.Label == "Title");
        title.Edit.Should().BeTrue();
        title.IsActive.Should().BeTrue();
        view.Lines.Should().ContainSingle(line =>
            line.Text == "Enter ACCEPT  Esc CANCEL" &&
            line.Role == TodoTaskEditorDialogRole.Hint);
        view.Lines.Should().NotContain(line => line.Text.Contains("TITLE", StringComparison.Ordinal));
    }

    [Fact]
    public void Create_embeds_a_read_only_title_textbox_while_browsing_the_form()
    {
        var view = TodoTaskEditorDialog.Create(Editor() with { SelectedIndex = (int)TodoFormField.Title }, Bindings, 80, 24);

        var title = view.TextBoxes!.Single(textBox => textBox.Label == "Title");
        title.Edit.Should().BeFalse();
        title.IsActive.Should().BeTrue();
        view.Lines.Should().NotContain(line => line.Text.Contains("TITLE", StringComparison.Ordinal));
    }

    [Fact]
    public void Create_embeds_a_reference_textbox_while_browsing_and_editing()
    {
        var reducer = new TodoEditorReducer();
        var editor = Editor() with { SelectedIndex = (int)TodoFormField.Reference };
        var browseView = TodoTaskEditorDialog.Create(editor, Bindings, 80, 24);
        var editing = reducer.Reduce(editor, Key('e'), Bindings, []).State!;
        var editView = TodoTaskEditorDialog.Create(editing, Bindings, 80, 24);

        var browseReference = browseView.TextBoxes!.Single(textBox => textBox.Label == "Reference");
        var editReference = editView.TextBoxes!.Single(textBox => textBox.Label == "Reference");
        browseReference.Edit.Should().BeFalse();
        browseReference.IsActive.Should().BeTrue();
        editReference.Edit.Should().BeTrue();
        editView.Lines.Should().NotContain(line => line.Text.Contains("REFERENCE", StringComparison.Ordinal));
    }

    [Fact]
    public void Create_scrolls_field_textboxes_to_keep_the_selected_field_visible()
    {
        var editor = Editor() with { SelectedIndex = (int)TodoFormField.Duration };

        var view = TodoTaskEditorDialog.Create(editor, Bindings, 80, 24);

        view.TextBoxes.Should().HaveCount(4);
        view.TextBoxes!.Select(textBox => textBox.Label).Should().Equal(
            "Tags",
            "Scheduled date (YYYY-MM-DD, t+1, w+1, mon)",
            "Scheduled time",
            "Duration");
        view.TextBoxes.Single(textBox => textBox.Label == "Duration").IsActive.Should().BeTrue();
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

    private static ConsoleKeyInfo Key(char character) => new(character, ConsoleKey.Oem2, false, false, false);
}
