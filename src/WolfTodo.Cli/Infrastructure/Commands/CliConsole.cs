using McMaster.Extensions.CommandLineUtils;

namespace WolfTodo.Cli.Infrastructure.Commands;

public sealed class CliConsole(TextReader input, TextWriter output) : IConsole
{
    public event ConsoleCancelEventHandler? CancelKeyPress
    {
        add { }
        remove { }
    }
    public TextWriter Out => output;
    public TextWriter Error => output;
    public TextReader In => input;
    public bool IsInputRedirected => true;
    public bool IsOutputRedirected => true;
    public bool IsErrorRedirected => true;
    public ConsoleColor ForegroundColor { get; set; }
    public ConsoleColor BackgroundColor { get; set; }

    public void ResetColor()
    {
        ForegroundColor = default;
        BackgroundColor = default;
    }
}
