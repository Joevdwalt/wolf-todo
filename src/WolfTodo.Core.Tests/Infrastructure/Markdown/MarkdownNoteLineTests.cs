using FluentAssertions;
using WolfTodo.Core.Infrastructure.Markdown;

namespace WolfTodo.Core.Tests.Infrastructure.Markdown;

public sealed class MarkdownNoteLineTests
{
    [Fact]
    public void Preserves_note_line_properties()
    {
        var line = new MarkdownNoteLine(4, 2, "Review contract", false, true);

        line.SourceLine.Should().Be(4);
        line.Indent.Should().Be(2);
        line.Text.Should().Be("Review contract");
        line.IsBlank.Should().BeFalse();
        line.IsListItem.Should().BeTrue();
    }
}
