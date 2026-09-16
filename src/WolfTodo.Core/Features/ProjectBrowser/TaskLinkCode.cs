using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace WolfTodo.Core.Features.ProjectBrowser;

public static class TaskLinkCode
{
    public const string InvalidCodeMessage = "Expected wt1- followed by 8 lowercase hexadecimal digits.";

    public static string Generate(string canonicalProjectPath, int sourceLine)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(sourceLine, 1);
        var input = canonicalProjectPath + "\n" + sourceLine.ToString(CultureInfo.InvariantCulture);
        return "wt1-" + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(input)))[..8];
    }

    public static bool IsValid(string? code) =>
        code is not null && code.Length == 12 && code.StartsWith("wt1-", StringComparison.Ordinal) &&
        code.AsSpan(4).IndexOfAnyExcept("0123456789abcdef") < 0;

    public static (TodoProject Project, TodoItem Todo)? Resolve(ProjectCatalog catalog, string code)
        => Resolve(catalog, code, out _);

    public static (TodoProject Project, TodoItem Todo)? Resolve(ProjectCatalog catalog, string code, out bool ambiguous)
    {
        var match = ResolveMatch(catalog, code, out ambiguous);
        return match is null ? null : (match.Project, match.Todo);
    }

    public static TaskLinkMatch? ResolveMatch(ProjectCatalog catalog, string code, out bool ambiguous)
    {
        ambiguous = false;
        if (!IsValid(code)) return null;
        TaskLinkMatch? match = null;
        foreach (var project in catalog.Projects)
        {
            foreach (var candidate in Flatten(project.Todos))
            {
                if (Generate(project.Path, candidate.Todo.SourceLine) != code) continue;
                if (match is not null)
                {
                    ambiguous = true;
                    return null;
                }
                match = new TaskLinkMatch(project, candidate.Todo, candidate.ParentSourceLine);
            }
        }
        return match;
    }

    private static IEnumerable<(TodoItem Todo, int? ParentSourceLine)> Flatten(
        IEnumerable<TodoItem> todos, int? parentSourceLine = null)
    {
        foreach (var todo in todos)
        {
            yield return (todo, parentSourceLine);
            foreach (var child in Flatten(todo.Subtasks, todo.SourceLine)) yield return child;
        }
    }
}
