namespace Wolf.Controls;

/// <summary>State for one transient toast notification.</summary>
public sealed record ToastState(string Message, ToastSeverity Severity, DateTimeOffset VisibleFrom, DateTimeOffset VisibleUntil)
{
    public bool IsVisibleAt(DateTimeOffset now) => now >= VisibleFrom && now < VisibleUntil;
}
