using WolfTodo.Cli.Features;

namespace WolfTodo.Cli.Infrastructure.Commands.Add;

public sealed class AddCommandHandler(
    TaskImportService importService,
    TaskUpdateFactory taskFactory,
    CliOutputWriter output)
{
    public int Execute(AddCommand command, IReadOnlyList<string> arguments)
    {
        var project = CommandOptionValues.RequiredSingle(command.Project, "--project");
        var result = importService.Import(project, [taskFactory.FromAdd(command, arguments)]);
        if (!result.Succeeded)
            return output.Error(1, result.ErrorCode!, result.Error!);

        output.Write(new
        {
            ok = true,
            project = new { title = result.ProjectTitle, path = result.ProjectPath },
            created_count = result.SourceLines.Count,
            created = result.SourceLines.Select((line, index) => new { index, source_line = line })
        });
        return 0;
    }
}
