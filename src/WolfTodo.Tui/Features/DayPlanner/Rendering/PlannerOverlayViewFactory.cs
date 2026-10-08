using WolfTodo.Tui.Features.Commands;
using WolfTodo.Tui.Features.TodoEditing;
using WolfTodo.Tui.Controls;
using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Features.ApplicationShell.Rendering;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.DayPlanner.Rendering;

public sealed class PlannerOverlayViewFactory
{
    private readonly StatusRenderer statusRenderer;

    public PlannerOverlayViewFactory(StatusRenderer statusRenderer)
    {
        this.statusRenderer = statusRenderer;
    }

    public TodoTaskEditorDialogView? CreatePlannerEditorDialog(
        PlannerView view,
        TuiKeyBindings keyBindings,
        int width,
        int height) =>
        view.State.Editor is { } editor
            ? TodoTaskEditorDialog.Create(editor, keyBindings, width, height)
            : null;

    public SelectListView? PlannerSelectList(PlannerView view, TuiKeyBindings bindings)
    {
        if (view.CommandPalette is not null)
        {
            return CommandPaletteSelectList(view.CommandPalette, bindings);
        }

        if (view.State.Editor is not null)
        {
            return TodoEditorSelectList(
                view.State.Editor,
                view.Projects.Select(project => new TodoEditorProjectOption(project.Title, project.Path)).ToArray(),
                bindings);
        }

        if (view.State.Mode is not (PlannerMode.ChooseTodo or PlannerMode.EditFilter))
        {
            return null;
        }

        var searchText = view.State.Mode == PlannerMode.EditFilter
            ? view.State.FilterDraft
            : view.State.FilterText.Length == 0 ? null : view.State.FilterText;
        return new SelectListView(
            "Unscheduled todos",
            view.PickerTodos
                .Select(todo => new SelectOption(todo.Todo.Title, $"[{todo.ProjectTitle}]"))
                .ToArray(),
            view.State.PickerIndex,
            searchText,
            "No open unscheduled todos",
            statusRenderer.PlannerPickerFooter(bindings),
            view.State.Error);
    }

    public SelectListView CommandPaletteSelectList(CommandPaletteView palette, TuiKeyBindings bindings) =>
        new(
            "Command palette",
            palette.Items.Select(item => new SelectOption(
                $"{item.Group}: {item.Label}",
                $"[{item.Binding}]" + (item.IsEnabled ? string.Empty : $" — {item.DisabledReason}"),
                item.IsEnabled)).ToArray(),
            palette.SelectedIndex,
            palette.State.IsSearching ? palette.State.Query : null,
            "No matching actions",
            statusRenderer.CommandPaletteFooter(bindings),
            palette.State.Error);

    public SelectListView? TodoEditorSelectList(
        TodoTaskEditorState editor,
        IReadOnlyList<TodoEditorProjectOption> projects,
        TuiKeyBindings bindings)
    {
        if (editor.IsEditingContent)
        {
            return null;
        }

        if (editor.IsChoosingProject)
        {
            return new SelectListView(
                "Choose project",
                projects.Select(project => new SelectOption(project.Title)).ToArray(),
                editor.ProjectPickerIndex,
                null,
                "No valid projects",
                $"{statusRenderer.Shortest(bindings.MoveDown)}/{statusRenderer.Shortest(bindings.MoveUp)} MOVE  " +
                $"{statusRenderer.Shortest(bindings.Open)} SELECT  {statusRenderer.Shortest(bindings.Back)} CANCEL",
                editor.Error);
        }

        return null;
    }

    public MultilineTextBoxState? PlannerTextBox(PlannerView view) =>
        view.State.Editor is null ? null : TodoEditorTextBox(view.State.Editor);

    public MultilineTextBoxState? TodoEditorTextBox(TodoTaskEditorState editor) =>
        editor.ContentTextBox;

}
