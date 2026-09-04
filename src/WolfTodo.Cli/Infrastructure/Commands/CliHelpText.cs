namespace WolfTodo.Cli.Infrastructure.Commands;

public static class CliHelpText
{
    public const string Text = """
                                Wolf Todo CLI

                                wtodo add --project <title|absolute-path> --title <text> [options]
                                wtodo import --file <path>
                                wtodo import --stdin
                                wtodo list [--project <title|absolute-path>]

                                Add options:
                                  --reference <text>
                                  --priority <lowest|low|medium|high|highest>
                                  --tag <tag>                         Repeatable
                                  --scheduled <YYYY-MM-DD>
                                  --time <HH:mm>
                                  --duration-minutes <minutes>
                                  --content <multiline-text>          Optional task content
                                  --subtask <title>                   Repeatable, unchecked
                                  --completed-subtask <title>         Repeatable, completed
                                """;
}
