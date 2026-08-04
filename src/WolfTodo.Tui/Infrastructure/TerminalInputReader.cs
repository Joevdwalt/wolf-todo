namespace WolfTodo.Tui.Infrastructure;

public sealed class TerminalInputReader
{
    private readonly Func<ConsoleKeyInfo> readKey;
    private readonly Func<bool> keyAvailable;
    private readonly Func<DateTime> utcNowProvider;
    private readonly Action<TimeSpan> sleep;

    public TerminalInputReader()
        : this(
            () => Console.ReadKey(intercept: true),
            () => Console.KeyAvailable,
            () => DateTime.UtcNow,
            Thread.Sleep)
    {
    }

    public TerminalInputReader(
        Func<ConsoleKeyInfo> readKey,
        Func<bool> keyAvailable,
        Func<DateTime> utcNowProvider,
        Action<TimeSpan> sleep)
    {
        this.readKey = readKey;
        this.keyAvailable = keyAvailable;
        this.utcNowProvider = utcNowProvider;
        this.sleep = sleep;
    }

    public ConsoleKeyInfo ReadKey() => readKey();

    public ConsoleKeyInfo? ReadKey(TimeSpan timeout)
    {
        var deadline = utcNowProvider() + timeout;
        while (utcNowProvider() < deadline)
        {
            if (keyAvailable())
            {
                return readKey();
            }

            var remaining = deadline - utcNowProvider();
            if (remaining > TimeSpan.Zero)
            {
                sleep(remaining < TimeSpan.FromMilliseconds(50)
                    ? remaining
                    : TimeSpan.FromMilliseconds(50));
            }
        }

        return null;
    }
}
