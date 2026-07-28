using FluentAssertions;
using WolfTodo.Core.Infrastructure.Markdown;

namespace WolfTodo.Core.Tests.Infrastructure.Markdown;

public sealed class MarkdownHeadingTests
{
    [Fact]
    public void Preserves_the_heading_level_and_title()
    {
        var heading = new MarkdownHeading(2, "Renewals");

        heading.Level.Should().Be(2);
        heading.Title.Should().Be("Renewals");
    }
}
