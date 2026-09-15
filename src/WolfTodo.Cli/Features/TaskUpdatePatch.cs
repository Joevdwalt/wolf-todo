using System.Collections.Immutable;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Cli.Features;

public sealed record TaskUpdatePatch
{
    public bool? Completed { get; init; }
    public bool HasTitle { get; init; }
    public string? Title { get; init; }
    public bool HasReference { get; init; }
    public string? Reference { get; init; }
    public bool ClearReference { get; init; }
    public bool HasPriority { get; init; }
    public TodoPriority? Priority { get; init; }
    public bool ClearPriority { get; init; }
    public bool HasTags { get; init; }
    public ImmutableArray<string> Tags { get; init; } = [];
    public bool ClearTags { get; init; }
    public bool HasScheduledDate { get; init; }
    public DateOnly? ScheduledDate { get; init; }
    public bool HasTime { get; init; }
    public TimeOnly? Time { get; init; }
    public bool ClearTime { get; init; }
    public bool ClearSchedule { get; init; }
    public bool HasDuration { get; init; }
    public int? DurationMinutes { get; init; }
    public bool ClearDuration { get; init; }
    public bool HasContent { get; init; }
    public string? Content { get; init; }
    public bool ClearContent { get; init; }

    public bool HasChanges =>
        Completed is not null || HasTitle || HasReference || ClearReference ||
        HasPriority || ClearPriority || HasTags || ClearTags || HasScheduledDate ||
        HasTime || ClearTime || ClearSchedule || HasDuration || ClearDuration ||
        HasContent || ClearContent;
}
