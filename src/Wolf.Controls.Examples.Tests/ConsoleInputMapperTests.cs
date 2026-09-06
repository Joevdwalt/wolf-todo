using FluentAssertions;
using Wolf.Controls;
using Wolf.Controls.Examples;

namespace Wolf.Controls.Examples.Tests;

public sealed class ConsoleInputMapperTests
{
    [Fact]
    public void ToControlInput_maps_terminal_editing_and_selection_keys()
    {
        ConsoleInputMapper.ToControlInput(new ConsoleKeyInfo('\0', ConsoleKey.LeftArrow, false, false, false)).Kind
            .Should().Be(ControlInputKind.MoveLeft);
        ConsoleInputMapper.ToControlInput(new ConsoleKeyInfo('\0', ConsoleKey.Enter, false, false, false)).Kind
            .Should().Be(ControlInputKind.Accept);
        ConsoleInputMapper.ToControlInput(new ConsoleKeyInfo('a', ConsoleKey.A, false, false, true)).Kind
            .Should().Be(ControlInputKind.SelectAll);
    }
}
