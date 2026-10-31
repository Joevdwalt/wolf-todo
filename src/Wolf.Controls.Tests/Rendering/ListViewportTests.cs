using FluentAssertions;
using Wolf.Controls.Rendering;

namespace Wolf.Controls.Tests.Rendering;

public sealed class ListViewportTests
{
    [Theory]
    [InlineData(0, 0, new int[0])]
    [InlineData(-1, 2, new int[0])]
    [InlineData(2, 0, new[] { 0, 1 })]
    [InlineData(2, 2, new[] { 1, 2 })]
    [InlineData(2, 4, new[] { 3, 4 })]
    [InlineData(2, int.MaxValue, new[] { 3, 4 })]
    [InlineData(2, int.MinValue, new[] { 0, 1 })]
    public void Fit_keeps_selection_visible_and_clamps_to_available_rows(int height, int selected, int[] expected)
    {
        ListViewport.Fit<int>([0, 1, 2, 3, 4], height, selected).Should().Equal(expected);
    }

    [Fact]
    public void Fit_reuses_rows_that_already_fit()
    {
        IReadOnlyList<int> rows = new[] { 1, 2 };
        ListViewport.Fit(rows, 5, 0).Should().BeSameAs(rows);
        ListViewport.Fit(Array.Empty<int>(), 3, 0).Should().BeEmpty();
    }
}
