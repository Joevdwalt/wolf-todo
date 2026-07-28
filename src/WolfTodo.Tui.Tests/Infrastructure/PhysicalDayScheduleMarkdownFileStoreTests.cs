using FluentAssertions;
using WolfTodo.Tui.Infrastructure;

namespace WolfTodo.Tui.Tests.Infrastructure;

public sealed class PhysicalDayScheduleMarkdownFileStoreTests
{
    [Fact]
    public void WriteAllTextAtomically_creates_parent_directories_and_writes_contents()
    {
        var root = Path.Combine(Path.GetTempPath(), $"wolf-todo-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "2026", "07", "Week - 29.md");

        try
        {
            var store = new PhysicalDayScheduleMarkdownFileStore();

            store.WriteAllTextAtomically(path, "# Weekly note");

            store.FileExists(path).Should().BeTrue();
            store.ReadAllText(path).Should().Be("# Weekly note");
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
