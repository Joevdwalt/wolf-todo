using FluentAssertions;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Tests.Features.Configuration;

public sealed class KeyGestureTests
{
    [Fact]
    public void Parse_matches_printable_characters_case_sensitively()
    {
        var gesture = KeyGesture.Parse("k");

        gesture.Matches(Key('k')).Should().BeTrue();
        gesture.Matches(Key('K', shift: true)).Should().BeFalse();
        gesture.DisplayName.Should().Be("k");
    }

    [Fact]
    public void Parse_matches_named_keys_and_modifiers_case_insensitively()
    {
        var gesture = KeyGesture.Parse("control+shift+k");

        gesture.Matches(Key('K', shift: true, control: true)).Should().BeTrue();
        gesture.Matches(Key('K', shift: true)).Should().BeFalse();
        gesture.DisplayName.Should().Be("Ctrl+Shift+K");
    }

    [Theory]
    [InlineData("")]
    [InlineData("Ctrl+")]
    [InlineData("Meta+K")]
    [InlineData("NotAKey")]
    [InlineData("Ctrl+Ctrl+K")]
    public void Parse_rejects_invalid_gestures(string value)
    {
        var action = () => KeyGesture.Parse(value);

        action.Should().Throw<FormatException>();
    }

    private static ConsoleKeyInfo Key(
        char character,
        bool shift = false,
        bool alt = false,
        bool control = false) => new(character, ConsoleKey.K, shift, alt, control);
}
