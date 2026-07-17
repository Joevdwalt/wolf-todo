using FluentAssertions;
using Spectre.Console;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Tests.Features.Configuration;

public sealed class TuiThemesTests
{
    [Theory]
    [InlineData("wolf")]
    [InlineData("WOLF")]
    [InlineData("classic")]
    [InlineData("mono")]
    public void TryGet_resolves_builtin_presets_case_insensitively(string name)
    {
        TuiThemes.TryGet(name, out var theme).Should().BeTrue();
        theme.Should().NotBeNull();
    }

    [Fact]
    public void Wolf_uses_the_semantic_palette()
    {
        TuiThemes.Wolf.Accent.Should().Be(new Color(95, 215, 255));
        TuiThemes.Wolf.Heading.Should().Be(new Color(255, 175, 95));
        TuiThemes.Wolf.Border.Should().Be(new Color(95, 135, 175));
        TuiThemes.Wolf.Error.Should().Be(new Color(255, 95, 95));
        TuiThemes.Wolf.Tag.Should().Be(new Color(95, 215, 175));
    }

    [Fact]
    public void Mono_uses_terminal_default_colors_for_every_role()
    {
        var theme = TuiThemes.Mono;

        new[]
        {
            theme.Text,
            theme.Accent,
            theme.Heading,
            theme.Border,
            theme.Muted,
            theme.Success,
            theme.Warning,
            theme.Error,
            theme.Tag,
            theme.Date
        }.Should().OnlyContain(color => color == Color.Default);
    }
}
