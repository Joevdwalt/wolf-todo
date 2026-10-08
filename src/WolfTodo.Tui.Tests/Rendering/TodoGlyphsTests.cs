using FluentAssertions;
using WolfTodo.Tui.Rendering;

namespace WolfTodo.Tui.Tests.Rendering;

public sealed class TodoGlyphsTests
{
    [Fact]
    public void TodoGlyphs_exposes_shared_status_selection_and_mark_glyphs()
    {
        TodoGlyphs.OpenTodoGlyph.Should().Be("◯");
        TodoGlyphs.CompletedTodoGlyph.Should().Be("✓");
        TodoGlyphs.SelectedGlyph.Should().Be(">");
        TodoGlyphs.MarkedGlyph.Should().Be("*");
    }
}
