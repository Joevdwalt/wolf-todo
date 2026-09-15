namespace WolfTodo.Cli.Features;

public sealed record TaskListEntry(
    TaskProjectInfo Project,
    int SourceLine,
    string TaskCode,
    int? ParentSourceLine,
    bool Completed,
    string? Reference,
    string Title,
    string? Priority,
    IReadOnlyList<string> Tags,
    string SectionPath,
    TaskScheduleInfo? Schedule,
    int? DurationMinutes,
    IReadOnlyList<string> Notes);
