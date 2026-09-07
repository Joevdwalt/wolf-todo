namespace Wolf.Controls.ProgressBar;

/// <summary>State for a determinate progress bar transitioning between two values.</summary>
public sealed record ProgressState(string Label, double PreviousValue, double Value, DateTimeOffset ChangedAt)
{
    public static ProgressState Create(string label, double value, DateTimeOffset now)
    {
        return new ProgressState(label, value, value, now);
    }
}
