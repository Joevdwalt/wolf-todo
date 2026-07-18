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
        var theme = TuiThemes.Wolf;

        theme.Background.Should().Be(new Color(9, 18, 27));
        theme.Surface.Should().Be(new Color(16, 28, 40));
        theme.Surface2.Should().Be(new Color(22, 36, 51));
        theme.Border.Should().Be(new Color(35, 55, 74));
        theme.BorderActive.Should().Be(new Color(53, 82, 107));
        theme.Text.Should().Be(new Color(216, 225, 232));
        theme.SecondaryText.Should().Be(new Color(162, 178, 193));
        theme.Muted.Should().Be(new Color(107, 124, 142));
        theme.Accent.Should().Be(new Color(242, 140, 40));
        theme.AccentBright.Should().Be(new Color(255, 177, 74));
        theme.Heading.Should().Be(theme.AccentBright);
        theme.Success.Should().Be(new Color(108, 191, 132));
        theme.Tag.Should().Be(theme.Success);
        theme.Warning.Should().Be(new Color(226, 182, 77));
        theme.Error.Should().Be(new Color(217, 108, 108));
        theme.Info.Should().Be(new Color(95, 168, 211));
        theme.Date.Should().Be(theme.Info);
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
            theme.Date,
            theme.Background,
            theme.Surface,
            theme.Surface2,
            theme.SecondaryText,
            theme.BorderActive,
            theme.AccentBright,
            theme.Info
        }.Should().OnlyContain(color => color == Color.Default);
    }

    [Fact]
    public void Classic_keeps_terminal_surfaces_and_cyan_active_emphasis()
    {
        var theme = TuiThemes.Classic;

        theme.Background.Should().Be(Color.Default);
        theme.Surface.Should().Be(Color.Default);
        theme.Surface2.Should().Be(Color.Default);
        theme.Accent.Should().Be(Color.Cyan);
        theme.AccentBright.Should().Be(Color.Cyan);
    }
}
