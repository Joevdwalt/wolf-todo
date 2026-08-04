using FluentAssertions;
using WolfTodo.Tui.Infrastructure;

namespace WolfTodo.Tui.Tests.Infrastructure;

public sealed class TerminalInputReaderTests
{
    [Fact]
    public void ReadKey_uses_the_configured_key_reader()
    {
        var expected = new ConsoleKeyInfo('x', ConsoleKey.X, false, false, false);
        var reader = new TerminalInputReader(
            () => expected,
            () => false,
            () => DateTime.UnixEpoch,
            _ => { });

        reader.ReadKey().Should().Be(expected);
    }

    [Fact]
    public void ReadKey_with_timeout_returns_available_key()
    {
        var expected = new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false);
        var now = DateTime.UnixEpoch;
        var reader = new TerminalInputReader(
            () => expected,
            () => true,
            () => now,
            _ => now = now.AddMilliseconds(50));

        reader.ReadKey(TimeSpan.FromSeconds(1)).Should().Be(expected);
    }

    [Fact]
    public void ReadKey_with_timeout_returns_null_after_deadline()
    {
        var now = DateTime.UnixEpoch;
        var reader = new TerminalInputReader(
            () => new ConsoleKeyInfo('x', ConsoleKey.X, false, false, false),
            () => false,
            () => now,
            delay => now = now.Add(delay));

        reader.ReadKey(TimeSpan.FromMilliseconds(75)).Should().BeNull();
    }
}
