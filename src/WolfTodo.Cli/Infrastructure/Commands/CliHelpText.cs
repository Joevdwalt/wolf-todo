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
                                  --note <text>                       Repeatable and ordered
                                  --subtask <title>                   Repeatable and ordered
                                  --completed-subtask <title>         Repeatable and ordered
                                """;
}
