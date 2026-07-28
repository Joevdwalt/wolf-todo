using FluentAssertions;
using WolfTodo.Tui.Controls;

namespace WolfTodo.Tui.Tests.Controls;

public sealed class MultilineTextBoxStateTests
{
    [Fact]
    public void Create_normalizes_windows_line_endings_and_places_the_cursor_at_the_end()
    {
        var state = MultilineTextBoxState.Create("Notes", "First\r\nSecond", isMultiline: true);

        state.Text.Should().Be("First\nSecond");
        state.Cursor.Should().Be(state.Text.Length);
        state.ClampedCursor.Should().Be(state.Text.Length);
    }

    [Fact]
    public void ClampedCursor_limits_the_cursor_to_the_text_bounds()
    {
        new MultilineTextBoxState("Notes", "Text", -1, true).ClampedCursor.Should().Be(0);
        new MultilineTextBoxState("Notes", "Text", 8, true).ClampedCursor.Should().Be(4);
    }
}
