using FluentAssertions;
using WolfTodo.Core.Features.ProjectBrowser;

namespace WolfTodo.Core.Tests.Features.ProjectBrowser;

public sealed class TaskLinkCodeTests
{
    [Fact]
    public void Generate_uses_the_versioned_sha8_of_path_lf_and_decimal_line()
    {
        TaskLinkCode.Generate("/todos/work.md", 3).Should().Be(
            "wt1-bfe8eb02");
        TaskLinkCode.Generate("/todos/work.md", 4).Should().NotBe(TaskLinkCode.Generate("/todos/work.md", 3));
        TaskLinkCode.Generate("/todos/other.md", 3).Should().NotBe(TaskLinkCode.Generate("/todos/work.md", 3));
        var invalid = () => TaskLinkCode.Generate("/todos/work.md", 0);
        invalid.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("wt1-abc")]
    [InlineData("wt2-00000000")]
    [InlineData("wt1-AAAAAAAA")]
    public void Resolve_rejects_malformed_codes(string code)
    {
        TaskLinkCode.IsValid(code).Should().BeFalse();
        TaskLinkCode.Resolve(new ProjectCatalog([], []), code).Should().BeNull();
    }

    [Fact]
    public void Resolve_finds_completed_descendants_and_the_current_location_occupant()
    {
        var child = Todo(4, "Child") with { IsCompleted = true };
        var project = new TodoProject("Work", "/todos/work.md", [Todo(3, "Parent") with { Subtasks = [child] }]);
        var code = TaskLinkCode.Generate(project.Path, 4);
        TaskLinkCode.Resolve(new ProjectCatalog([project], []), code)!.Value.Todo.Should().Be(child);
        var replacement = Todo(4, "Replacement");
        var changed = project with { Title = "Renamed", Todos = [replacement] };
        TaskLinkCode.Resolve(new ProjectCatalog([changed], []), code)!.Value.Todo.Should().Be(replacement);
        TaskLinkCode.Resolve(new ProjectCatalog([changed with { Path = "/todos/moved.md" }], []), code).Should().BeNull();
        TaskLinkCode.Resolve(new ProjectCatalog([], []), code).Should().BeNull();
    }

    [Fact]
    public void Resolve_rejects_colliding_short_codes_instead_of_selecting_the_first_task()
    {
        var project = new TodoProject("Work", "/todos/work.md", [Todo(66654, "First"), Todo(82819, "Second")]);
        var code = TaskLinkCode.Generate(project.Path, 66654);
        code.Should().Be(TaskLinkCode.Generate(project.Path, 82819));
        TaskLinkCode.Resolve(new ProjectCatalog([project], []), code, out var ambiguous).Should().BeNull();
        ambiguous.Should().BeTrue();
    }

    private static TodoItem Todo(int line, string title) =>
        new(line, false, null, title, null, [], null, null, "", [], []);
}
