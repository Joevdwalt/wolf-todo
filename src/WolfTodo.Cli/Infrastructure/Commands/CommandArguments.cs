namespace WolfTodo.Cli.Infrastructure.Commands;

public static class CommandArguments
{
    public static IReadOnlyList<ContentInput> OrderedContent(IReadOnlyList<string> arguments)
    {
        var commandIndex = arguments.ToList().FindIndex(value =>
            string.Equals(value, "add", StringComparison.OrdinalIgnoreCase));
        var content = new List<ContentInput>();
        for (var index = Math.Max(commandIndex + 1, 0); index < arguments.Count; index++)
        {
            var option = arguments[index];
            if (option is "--note" or "--subtask" or "--completed-subtask")
            {
                if (index + 1 >= arguments.Count)
                    continue;
                var value = arguments[++index];
                content.Add(new ContentInput
                {
                    Type = option == "--note" ? "note" : "subtask",
                    Text = option == "--note" ? value : null,
                    Title = option == "--note" ? null : value,
                    Completed = option == "--completed-subtask" ? true : option == "--subtask" ? false : null
                });
            }
        }

        return content;
    }
}
