using System.Collections.Immutable;
using Tomlyn;
using Tomlyn.Model;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Infrastructure;

public sealed class TomlApplicationConfigurationLoader(
    string path,
    Func<string, bool> fileExists,
    Func<string, string> readAllText) : IApplicationConfigurationLoader
{
    public ApplicationConfiguration Load()
    {
        if (!fileExists(path))
        {
            throw new InvalidDataException($"Missing required configuration file: {path}");
        }

        TomlTable document;

        try
        {
            document = Toml.ToModel(readAllText(path));
        }
        catch (Exception exception)
        {
            throw new InvalidDataException($"Invalid configuration file: {exception.Message}", exception);
        }

        var files = ReadProjectFiles(document);
        var bindings = ReadKeyBindings(document);
        return new ApplicationConfiguration(files, bindings);
    }

    private static ImmutableArray<string> ReadProjectFiles(TomlTable document)
    {
        if (!document.TryGetValue("projects", out var projectsValue) ||
            projectsValue is not TomlTable projects ||
            !projects.TryGetValue("files", out var filesValue) ||
            filesValue is not TomlArray files ||
            files.Count == 0)
        {
            throw new InvalidDataException(
                "Invalid configuration file: projects.files must contain at least one absolute Markdown file path.");
        }

        var result = ImmutableArray.CreateBuilder<string>();

        foreach (var fileValue in files)
        {
            if (fileValue is not string file ||
                string.IsNullOrWhiteSpace(file) ||
                !Path.IsPathFullyQualified(file) ||
                !string.Equals(Path.GetExtension(file), ".md", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "Invalid configuration file: every projects.files value must be an absolute .md file path.");
            }

            result.Add(file);
        }

        return result.ToImmutable();
    }

    private static TuiKeyBindings ReadKeyBindings(TomlTable document)
    {
        if (!document.TryGetValue("keybindings", out var bindingsValue) ||
            bindingsValue is not TomlTable keybindings)
        {
            throw new InvalidDataException(
                "Invalid configuration file: keybindings must be a TOML table.");
        }

        if (!keybindings.TryGetValue("quit", out var quitValue) ||
            quitValue is not string quitCommand ||
            string.IsNullOrWhiteSpace(quitCommand))
        {
            throw new InvalidDataException(
                "Invalid configuration file: keybindings.quit must be a non-empty string.");
        }

        var defaults = TuiKeyBindings.CreateDefaults(quitCommand);
        var completedCommand = ReadOptionalCommand(
            keybindings,
            "toggle_completed",
            defaults.ToggleCompletedCommand);
        var result = defaults with
        {
            ToggleCompletedCommand = completedCommand,
            MoveUp = ReadGestures(keybindings, "move_up", defaults.MoveUp),
            MoveDown = ReadGestures(keybindings, "move_down", defaults.MoveDown),
            FocusNext = ReadGestures(keybindings, "focus_next", defaults.FocusNext),
            FocusPrevious = ReadGestures(keybindings, "focus_previous", defaults.FocusPrevious),
            Open = ReadGestures(keybindings, "open", defaults.Open),
            Back = ReadGestures(keybindings, "back", defaults.Back),
            CommandMode = ReadGestures(keybindings, "command_mode", defaults.CommandMode),
            FilterMode = ReadGestures(keybindings, "filter_mode", defaults.FilterMode),
            SortMode = ReadGestures(keybindings, "sort_mode", defaults.SortMode),
            TabNext = ReadGestures(keybindings, "tab_next", defaults.TabNext),
            TabPrevious = ReadGestures(keybindings, "tab_previous", defaults.TabPrevious)
        };

        ValidateCommands(result);
        ValidateGestureConflicts(result);
        return result;
    }

    private static string ReadOptionalCommand(TomlTable keybindings, string name, string defaultValue)
    {
        if (!keybindings.TryGetValue(name, out var value))
        {
            return defaultValue;
        }

        if (value is not string command || string.IsNullOrWhiteSpace(command))
        {
            throw new InvalidDataException(
                $"Invalid configuration file: keybindings.{name} must be a non-empty string.");
        }

        return command;
    }

    private static ImmutableArray<KeyGesture> ReadGestures(
        TomlTable keybindings,
        string name,
        ImmutableArray<KeyGesture> defaults)
    {
        if (!keybindings.TryGetValue(name, out var value))
        {
            return defaults;
        }

        if (value is not TomlArray gestures || gestures.Count == 0)
        {
            throw new InvalidDataException(
                $"Invalid configuration file: keybindings.{name} must be a non-empty string array.");
        }

        var result = ImmutableArray.CreateBuilder<KeyGesture>();

        foreach (var gestureValue in gestures)
        {
            if (gestureValue is not string gestureText)
            {
                throw new InvalidDataException(
                    $"Invalid configuration file: every keybindings.{name} value must be a key gesture string.");
            }

            try
            {
                result.Add(KeyGesture.Parse(gestureText));
            }
            catch (FormatException exception)
            {
                throw new InvalidDataException(
                    $"Invalid configuration file: keybindings.{name} contains {exception.Message}",
                    exception);
            }
        }

        if (result.Distinct().Count() != result.Count)
        {
            throw new InvalidDataException(
                $"Invalid configuration file: keybindings.{name} contains a duplicate key gesture.");
        }

        return result.ToImmutable();
    }

    private static void ValidateCommands(TuiKeyBindings bindings)
    {
        if (bindings.QuitCommand == bindings.ToggleCompletedCommand)
        {
            throw new InvalidDataException(
                "Invalid configuration file: keybindings.quit and keybindings.toggle_completed must be different.");
        }
    }

    private static void ValidateGestureConflicts(TuiKeyBindings bindings)
    {
        var actions = new (string Name, ImmutableArray<KeyGesture> Gestures)[]
        {
            ("move_up", bindings.MoveUp),
            ("move_down", bindings.MoveDown),
            ("focus_next", bindings.FocusNext),
            ("focus_previous", bindings.FocusPrevious),
            ("open", bindings.Open),
            ("back", bindings.Back),
            ("command_mode", bindings.CommandMode),
            ("filter_mode", bindings.FilterMode),
            ("sort_mode", bindings.SortMode),
            ("tab_next", bindings.TabNext),
            ("tab_previous", bindings.TabPrevious)
        };
        var owners = new Dictionary<KeyGesture, string>();

        foreach (var action in actions)
        {
            foreach (var gesture in action.Gestures)
            {
                if (owners.TryGetValue(gesture, out var owner))
                {
                    throw new InvalidDataException(
                        $"Invalid configuration file: key gesture '{gesture.DisplayName}' is assigned to both " +
                        $"keybindings.{owner} and keybindings.{action.Name}.");
                }

                owners.Add(gesture, action.Name);
            }
        }
    }
}
