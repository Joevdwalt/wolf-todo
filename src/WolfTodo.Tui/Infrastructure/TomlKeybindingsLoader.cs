using Tomlyn;
using Tomlyn.Model;
using WolfTodo.Tui.Features.Splash;

namespace WolfTodo.Tui.Infrastructure;

public sealed class TomlKeybindingsLoader(string path, Func<string, string> readAllText) : IKeybindingsLoader
{
    public Keybindings Load()
    {
        if (!File.Exists(path))
        {
            throw new InvalidDataException($"Missing required keybindings file: {path}");
        }

        TomlTable document;

        try
        {
            document = Toml.ToModel(readAllText(path));
        }
        catch (Exception exception)
        {
            throw new InvalidDataException($"Invalid keybindings file: {exception.Message}", exception);
        }

        if (!document.TryGetValue("keybindings", out var bindingsValue) ||
            bindingsValue is not TomlTable keybindings ||
            !keybindings.TryGetValue("quit", out var quitValue) ||
            quitValue is not string quitCommand ||
            string.IsNullOrWhiteSpace(quitCommand))
        {
            throw new InvalidDataException("Invalid keybindings file: keybindings.quit must be a non-empty string.");
        }

        return new Keybindings(quitCommand);
    }
}
