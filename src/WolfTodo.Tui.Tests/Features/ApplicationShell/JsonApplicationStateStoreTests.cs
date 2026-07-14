using FluentAssertions;
using WolfTodo.Tui.Features.ApplicationShell;

namespace WolfTodo.Tui.Tests.Features.ApplicationShell;

public sealed class JsonApplicationStateStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        $"wolf-todo-state-tests-{Guid.NewGuid():N}");

    [Fact]
    public void Save_and_load_round_trip_the_selected_project_path()
    {
        var store = CreateStore();

        store.SaveSelectedProjectPath("/projects/work.md");

        store.LoadSelectedProjectPath().Should().Be("/projects/work.md");
    }

    [Fact]
    public void Save_and_load_round_trip_the_all_project_selection()
    {
        var store = CreateStore();

        store.SaveSelectedProjectPath(null);

        store.LoadSelectedProjectPath().Should().BeNull();
    }

    [Fact]
    public void Load_returns_all_when_the_state_file_is_missing_or_malformed()
    {
        var store = CreateStore();

        store.LoadSelectedProjectPath().Should().BeNull();

        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "state.json"), "not json");

        store.LoadSelectedProjectPath().Should().BeNull();
    }

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private JsonApplicationStateStore CreateStore() =>
        new(Path.Combine(directory, "state.json"));
}
