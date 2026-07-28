using FluentAssertions;
using WolfTodo.Tui.Controls;

namespace WolfTodo.Tui.Tests.Controls;

public sealed class TuiComponentConstraintsTests
{
    [Fact]
    public void Clamped_values_provide_at_least_one_column_and_row()
    {
        var constraints = new TuiComponentConstraints(-4, 0);

        constraints.ClampedWidth.Should().Be(1);
        constraints.ClampedMaxRows.Should().Be(1);
    }
}
