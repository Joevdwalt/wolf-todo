using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Tui.Features.ProjectBrowser;

public sealed record TodoBulkEditorTransition(
    TodoBulkEditorState? State,
    TodoBulkEditorOutcome Outcome = TodoBulkEditorOutcome.Editing,
    TodoBulkUpdate? Update = null);
