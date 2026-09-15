using WolfTodo.Cli.Features;

namespace WolfTodo.Cli.Infrastructure.Commands.Update;

public sealed class UpdateCommandHandler(
    TaskUpdateService updateService,
    TaskUpdatePatchFactory patchFactory,
    CliOutputWriter output)
{
    public int Execute(UpdateCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.Code))
        {
            return output.Error(2, "missing_argument", "Command update requires a task-code argument.");
        }

        var patch = patchFactory.Create(command);
        var result = updateService.Update(command.Code, patch);
        if (!result.Succeeded)
        {
            var exitCode = result.ErrorCode is "invalid_task_code" or "invalid_task" or
                "missing_update" or "duplicate_option" or "conflicting_options" ? 2 : 1;
            return output.Error(exitCode, result.ErrorCode!, result.Error!);
        }

        var match = result.Match!;
        output.Write(new
        {
            ok = true,
            task = TaskListEntryFactory.Create(match.Project, match.Todo, match.ParentSourceLine)
        });
        return 0;
    }
}
