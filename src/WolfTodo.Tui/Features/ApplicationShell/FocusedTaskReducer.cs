using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ApplicationShell;

public sealed class FocusedTaskReducer(Func<DateOnly>? todayProvider = null)
{
    private readonly TodoEditorReducer editorReducer = new(todayProvider);

    public FocusedTaskTransition ReduceAction(
        FocusedTaskState state,
        FocusedTaskAction action,
        FocusedTaskView view) => action switch
    {
        FocusedTaskAction.Edit => Edit(state, view),
        FocusedTaskAction.EditExternal => Operate(state, view, FocusedTaskOperation.EditExternal),
        FocusedTaskAction.ToggleCompleted => Operate(state, view, FocusedTaskOperation.ToggleCompleted),
        FocusedTaskAction.JumpTop => Select(state, view, 0),
        FocusedTaskAction.JumpBottom => Select(state, view, view.Items.Length - 1),
        FocusedTaskAction.Exit => new(state, FocusedTaskOperation.Exit),
        _ => new(state)
    };

    public FocusedTaskTransition Reduce(
        FocusedTaskState state,
        ConsoleKeyInfo key,
        TuiKeyBindings bindings,
        FocusedTaskView view)
    {
        state = state with { Error = null, StatusMessage = null };
        if (state.Editor is not null)
        {
            var transition = editorReducer.Reduce(state.Editor, key, bindings, view.Projects);
            return new FocusedTaskTransition(
                state with
                {
                    Editor = transition.Operation == TodoEditorOperation.None ? transition.State : state.Editor
                },
                transition.Operation switch
                {
                    TodoEditorOperation.Update => FocusedTaskOperation.Update,
                    _ => FocusedTaskOperation.None
                },
                transition.Target,
                transition.Update,
                state.Editor.ExpectedTodo);
        }

        if (bindings.MatchesEditTodo(key) || bindings.MatchesEditTodoContent(key))
        {
            return Edit(state, view);
        }

        if (bindings.MatchesEditTodoExternal(key))
        {
            return Operate(state, view, FocusedTaskOperation.EditExternal);
        }

        if (bindings.MatchesToggleTodo(key))
        {
            return Operate(state, view, FocusedTaskOperation.ToggleCompleted);
        }

        if (bindings.MatchesJumpTop(key)) return Select(state, view, 0);
        if (bindings.MatchesJumpBottom(key)) return Select(state, view, view.Items.Length - 1);
        if (bindings.MatchesMoveUp(key) || bindings.MatchesMoveDown(key))
        {
            var current = Array.FindIndex(view.Items.ToArray(), item => item.IsSelected);
            var offset = bindings.MatchesMoveUp(key) ? -1 : 1;
            return Select(state, view, Math.Clamp(current + offset, 0, view.Items.Length - 1));
        }

        return bindings.MatchesBack(key)
            ? new FocusedTaskTransition(state, FocusedTaskOperation.Exit)
            : new FocusedTaskTransition(state);
    }

    private FocusedTaskTransition Edit(FocusedTaskState state, FocusedTaskView view) => new(
        state with { Editor = editorReducer.EditEditor(view.SelectedItem.Todo, view.SelectedItem.Identity) });

    private static FocusedTaskTransition Operate(
        FocusedTaskState state,
        FocusedTaskView view,
        FocusedTaskOperation operation) => new(
        state,
        operation,
        view.SelectedItem.Identity,
        ExpectedTodo: view.SelectedItem.Todo);

    private static FocusedTaskTransition Select(
        FocusedTaskState state,
        FocusedTaskView view,
        int index) => new(state with { SelectedIdentity = view.Items[index].Identity });
}
