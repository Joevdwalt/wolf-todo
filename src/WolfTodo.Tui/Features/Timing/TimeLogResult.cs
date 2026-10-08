namespace WolfTodo.Tui.Features.Timing;

public sealed record TimeLogResult(bool Succeeded, string? Error = null)
{
    public static TimeLogResult Success() => new(true);
    public static TimeLogResult Failure(string error) => new(false, error);
}
