namespace WolfTodo.Cli.Infrastructure.Commands;

public static class CliHelpText
{
    public const string Text = """
                                Wolf Todo CLI

                                wtodo add --project <title|absolute-path> --title <text> [options]
                                wtodo import --file <path>
                                wtodo import --stdin
                                wtodo list [--project <title|absolute-path>]
                                wtodo get <task-code>
                                wtodo update <task-code> [options]

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

                                Update options:
                                  --completed <true|false>
                                  --title <text>
                                  --reference <text> | --clear-reference
                                  --priority <lowest|low|medium|high|highest> | --clear-priority
                                  --tag <tag>                         Repeatable, replaces tags
                                  --clear-tags
                                  --scheduled <YYYY-MM-DD>
                                  --time <HH:mm> | --clear-time
                                  --clear-schedule
                                  --duration-minutes <minutes> | --clear-duration
                                  --content <multiline-text> | --clear-content
                                """;
}
