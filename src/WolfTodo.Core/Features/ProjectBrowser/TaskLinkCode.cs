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

    public static bool IsValid(string code) =>
        code.Length == 12 && code.StartsWith("wt1-", StringComparison.Ordinal) &&
        code.AsSpan(4).IndexOfAnyExcept("0123456789abcdef") < 0;

    public static (TodoProject Project, TodoItem Todo)? Resolve(ProjectCatalog catalog, string code)
        => Resolve(catalog, code, out _);

    public static (TodoProject Project, TodoItem Todo)? Resolve(ProjectCatalog catalog, string code, out bool ambiguous)
    {
        ambiguous = false;
        if (!IsValid(code)) return null;
        (TodoProject Project, TodoItem Todo)? match = null;
        foreach (var project in catalog.Projects)
        {
            foreach (var todo in Flatten(project.Todos))
            {
                if (Generate(project.Path, todo.SourceLine) != code) continue;
                if (match is not null)
                {
                    ambiguous = true;
                    return null;
                }
                match = (project, todo);
            }
        }
        return match;
    }

    private static IEnumerable<TodoItem> Flatten(IEnumerable<TodoItem> todos)
    {
        foreach (var todo in todos)
        {
            yield return todo;
            foreach (var child in Flatten(todo.Subtasks)) yield return child;
        }
    }
}
