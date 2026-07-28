namespace WolfTodo.Tui.Features.DayPlanner;

public sealed record DayScheduleExportResult(bool Succeeded, string? Path = null, string? Error = null)
{
    public static DayScheduleExportResult Success(string path) => new(true, path);

    public static DayScheduleExportResult Failure(string error) => new(false, null, error);
}
