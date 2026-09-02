using McMaster.Extensions.CommandLineUtils;
using Microsoft.Extensions.DependencyInjection;
using WolfTodo.Cli.Infrastructure.Commands;
using WolfTodo.Cli.Infrastructure.Commands.Add;
using WolfTodo.Cli.Infrastructure.Commands.Import;
using WolfTodo.Cli.Infrastructure.Commands.List;

namespace WolfTodo.Cli;

public sealed class CliApplication
{
    private readonly TextReader input;
    private readonly TextWriter output;
    private readonly CliOutputWriter outputWriter;
    private readonly AddCommandHandler addHandler;
    private readonly ImportCommandHandler importHandler;
    private readonly ListCommandHandler listHandler;

    public CliApplication(
        Features.TaskImportService importService,
        Features.TaskListService listService,
        TextReader input,
        TextWriter output,
        Func<string, string> readAllText)
    {
        this.input = input;
        this.output = output;
        outputWriter = new CliOutputWriter(output);
        var taskFactory = new TaskUpdateFactory();
        addHandler = new AddCommandHandler(importService, taskFactory, outputWriter);
        importHandler = new ImportCommandHandler(importService, taskFactory, outputWriter, input, readAllText);
        listHandler = new ListCommandHandler(listService, outputWriter);
    }

    public int Run(string[] args)
    {
        try
        {
            if (args.Length == 0 || args is ["--help"] or ["-h"] or ["help"])
            {
                output.WriteLine(CliHelpText.Text);
                return 0;
            }

            var console = new CliConsole(input, output);
            using var services = new ServiceCollection()
                .AddSingleton(addHandler)
                .AddSingleton(importHandler)
                .AddSingleton(listHandler)
                .AddSingleton(new CliInvocation(args))
                .AddSingleton<IConsole>(console)
                .BuildServiceProvider();

            using var application = new CommandLineApplication<RootCommand>(console);
            application.Conventions
                .UseDefaultConventions()
                .UseConstructorInjection(services);
            application.MakeSuggestionsInErrorMessage = false;
            return application.Execute(args);
        }
        catch (CommandParsingException exception)
        {
            var mapped = CommandParsingErrorMapper.Map(args, exception);
            return outputWriter.Error(2, mapped.Code, mapped.Message);
        }
        catch (CommandException exception)
        {
            return outputWriter.Error(2, exception.Code, exception.Message);
        }
        catch (Exception exception) when (
            exception is InvalidDataException or IOException or UnauthorizedAccessException or NotSupportedException)
        {
            return outputWriter.Error(1, "operation_failed", exception.Message);
        }
    }
}
