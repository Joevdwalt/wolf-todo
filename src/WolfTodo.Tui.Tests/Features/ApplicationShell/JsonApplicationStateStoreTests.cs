using FluentAssertions;
using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Features.ProjectBrowser;

namespace WolfTodo.Tui.Tests.Features.ApplicationShell;

public sealed class JsonApplicationStateStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(
        Path.GetTempPath(),
        $"wolf-todo-state-tests-{Guid.NewGuid():N}");

    [Fact]
    public void Save_and_load_round_trip_the_selected_project_and_sort()
    {
        var store = CreateStore();

        var state = new ApplicationSessionState(
            "/projects/work.md",
            new TodoSort(TodoSortProperty.Priority, TodoSortDirection.Descending));
        store.Save(state);

        store.Load().Should().Be(state);
    }

    [Fact]
    public void Save_and_load_round_trip_the_all_project_selection()
    {
        var store = CreateStore();

        store.Save(ApplicationSessionState.Initial);

        store.Load().Should().Be(ApplicationSessionState.Initial);
    }

    [Fact]
    public void Load_returns_all_when_the_state_file_is_missing_or_malformed()
    {
        var store = CreateStore();

        store.Load().Should().Be(ApplicationSessionState.Initial);

        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "state.json"), "not json");

        store.Load().Should().Be(ApplicationSessionState.Initial);
    }

    [Fact]
    public void Load_reads_legacy_path_only_state_with_source_sorting()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, "state.json"),
            "{\"SelectedProjectPath\":\"/projects/work.md\"}");

        var result = CreateStore().Load();

        result.SelectedProjectPath.Should().Be("/projects/work.md");
        result.Sort.Should().Be(TodoSort.Source);
    }

    [Fact]
    public void Load_preserves_existing_numeric_sort_property_values()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, "state.json"),
            "{\"SelectedProjectPath\":\"/projects/work.md\"," +
            "\"Sort\":{\"Property\":4,\"Direction\":1}}");

        var result = CreateStore().Load();

        result.Sort.Should().Be(new TodoSort(TodoSortProperty.File, TodoSortDirection.Descending));
    }

    [Fact]
    public void Load_maps_the_legacy_start_date_sort_position_to_schedule()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, "state.json"),
            "{\"Sort\":{\"Property\":2,\"Direction\":0}}");

        var result = CreateStore().Load();

        result.Sort.Should().Be(new TodoSort(TodoSortProperty.Schedule, TodoSortDirection.Ascending));
    }

    [Fact]
    public void Load_keeps_the_project_and_defaults_an_unknown_sort()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path.Combine(directory, "state.json"),
            "{\"SelectedProjectPath\":\"/projects/work.md\"," +
            "\"Sort\":{\"Property\":999,\"Direction\":0}}");

        var result = CreateStore().Load();

        result.SelectedProjectPath.Should().Be("/projects/work.md");
        result.Sort.Should().Be(TodoSort.Source);
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
