using FluentAssertions;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Tests.Features.ProjectBrowser;

public sealed class SortDialogKeysTests
{
    [Theory]
    [InlineData('n', TodoSortProperty.Name, TodoSortDirection.Ascending)]
    [InlineData('N', TodoSortProperty.Name, TodoSortDirection.Descending)]
    [InlineData('d', TodoSortProperty.Schedule, TodoSortDirection.Ascending)]
    [InlineData('D', TodoSortProperty.Schedule, TodoSortDirection.Descending)]
    [InlineData('p', TodoSortProperty.Priority, TodoSortDirection.Ascending)]
    [InlineData('P', TodoSortProperty.Priority, TodoSortDirection.Descending)]
    [InlineData('t', TodoSortProperty.Tags, TodoSortDirection.Ascending)]
    [InlineData('T', TodoSortProperty.Tags, TodoSortDirection.Descending)]
    [InlineData('f', TodoSortProperty.File, TodoSortDirection.Ascending)]
    [InlineData('F', TodoSortProperty.File, TodoSortDirection.Descending)]
    [InlineData('o', TodoSortProperty.Source, TodoSortDirection.Ascending)]
    public void Resolve_returns_the_sort_for_each_dialog_option(
        char character, TodoSortProperty property, TodoSortDirection direction)
    {
        var key = new ConsoleKeyInfo(character, ConsoleKey.Oem2, false, false, false);

        SortDialogKeys.Resolve(key).Should().Be(new TodoSort(property, direction));
    }

    [Theory]
    [InlineData('O')]
    [InlineData('x')]
    [InlineData('\0')]
    public void Resolve_returns_null_for_unrecognized_input(char character)
    {
        var key = new ConsoleKeyInfo(character, ConsoleKey.Oem2, false, false, false);

        SortDialogKeys.Resolve(key).Should().BeNull();
    }
}
