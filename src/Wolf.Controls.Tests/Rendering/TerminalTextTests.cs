using FluentAssertions;
using Spectre.Console;
using Wolf.Controls.Rendering;

namespace Wolf.Controls.Tests.Rendering;

public sealed class TerminalTextTests
{
    [Theory]
    [InlineData("alpha", 0, "")]
    [InlineData("alpha", -1, "")]
    [InlineData("alpha", 1, "…")]
    [InlineData("alpha", 4, "alp…")]
    [InlineData("alpha", 5, "alpha")]
    [InlineData("", 1, "")]
    [InlineData("狼狼狼", 4, "狼…")]
    [InlineData("e\u0301long", 2, "e\u0301…")]
    public void Ellipsize_fits_terminal_cells(string text, int width, string expected)
    {
        var result = TerminalText.Ellipsize(text, width);
        result.Should().Be(expected);
        result.GetCellWidth().Should().BeLessThanOrEqualTo(Math.Max(0, width));
    }

    [Fact]
    public void Clip_preserves_complete_emoji_text_elements()
    {
        const string family = "👨‍👩‍👧‍👦";
        var width = family.GetCellWidth();
        TerminalText.Clip(family + "more", width).Should().Be(family);
        TerminalText.Clip(family + "more", width - 1).Should().BeEmpty();
    }

    [Theory]
    [InlineData("狼", 4, "狼  ")]
    [InlineData("long", 3, "lo…")]
    [InlineData("x", 0, "")]
    public void PadColumn_fills_exact_cell_width(string text, int width, string expected)
    {
        var result = TerminalText.PadColumn(text, width);
        result.Should().Be(expected);
        result.GetCellWidth().Should().Be(width);
    }
}
