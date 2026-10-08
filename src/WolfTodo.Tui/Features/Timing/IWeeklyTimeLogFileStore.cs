namespace WolfTodo.Tui.Features.Timing;

public interface IWeeklyTimeLogFileStore
{
    bool FileExists(string path);
    string ReadAllText(string path);
    void WriteAllTextAtomically(string path, string contents);
}
