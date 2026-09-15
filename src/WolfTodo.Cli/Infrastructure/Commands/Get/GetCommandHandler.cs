using WolfTodo.Cli.Features;

namespace WolfTodo.Cli.Infrastructure.Commands.Get;

public sealed class GetCommandHandler(TaskListService listService, CliOutputWriter output)
{
    public int Execute(GetCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Code))
        {
            return output.Error(2, "missing_argument", "Command get requires a task-code argument.");
        }

        var result = listService.Get(command.Code);
        if (!result.Succeeded)
        {
            return output.Error(result.ErrorCode == "invalid_task_code" ? 2 : 1,
                result.ErrorCode!, result.Error!);
        }

        var match = result.Match!;
        var task = TaskListEntryFactory.Create(match.Project, match.Todo, match.ParentSourceLine);
        output.Write(new { ok = true, task });
        return 0;
    }
}
