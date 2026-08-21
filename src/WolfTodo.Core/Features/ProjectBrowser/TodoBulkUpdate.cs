using System.Collections.Immutable;

namespace WolfTodo.Core.Features.ProjectBrowser;

public sealed record TodoBulkUpdate(
    TodoBulkScheduleMode ScheduleMode,
    DateOnly? ScheduledDate,
    TodoBulkTagMode TagMode,
    ImmutableArray<string> Tags,
    TodoBulkPriorityMode PriorityMode,
    TodoPriority? Priority,
    bool Complete)
{
    public bool HasChanges =>
        ScheduleMode != TodoBulkScheduleMode.Unchanged ||
        TagMode != TodoBulkTagMode.Unchanged ||
        PriorityMode != TodoBulkPriorityMode.Unchanged ||
        Complete;
}
