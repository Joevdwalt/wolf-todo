using FluentAssertions;
using WolfTodo.Tui.Controls;

namespace WolfTodo.Tui.Tests.Controls;

public sealed class SelectListViewTests
{
    [Fact]
    public void ClampedSelectedIndex_limits_the_selection_to_available_options()
    {
        var options = new[] { new SelectOption("One"), new SelectOption("Two") };

        Create(options, -1).ClampedSelectedIndex.Should().Be(0);
        Create(options, 4).ClampedSelectedIndex.Should().Be(1);
    }

    [Fact]
    public void ClampedSelectedIndex_is_zero_when_no_options_are_available()
    {
        Create([], 4).ClampedSelectedIndex.Should().Be(0);
    }

    private static SelectListView Create(IReadOnlyList<SelectOption> options, int selectedIndex) => new(
        "Choose", options, selectedIndex, null, "No options", "Enter SELECT");
}
