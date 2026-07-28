namespace WolfTodo.Tui.Features.DayPlanner;

public interface IDayScheduleMarkdownFileStore
{
    bool FileExists(string path);

    string ReadAllText(string path);

    void WriteAllTextAtomically(string path, string contents);
}
