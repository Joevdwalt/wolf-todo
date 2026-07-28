using System.Collections.Immutable;

namespace WolfTodo.Tui.Features.Configuration;

public sealed record DayScheduleExportConfiguration(
    string NotesDirectory,
    ImmutableArray<string> ProjectLinks);
