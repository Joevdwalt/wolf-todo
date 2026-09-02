namespace WolfTodo.Cli.Infrastructure.Commands;

public interface ICliCommandRunner
{
    int RunAdd(string[] args);
    int RunImport(string[] args);
    int RunList(string[] args);
}
