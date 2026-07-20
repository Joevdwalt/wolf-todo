using System.Collections.Immutable;
using System.Reflection;
using Spectre.Console;
using Tomlyn;
using Tomlyn.Model;
using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Infrastructure;

public sealed class TomlApplicationConfigurationLoader(
    string path,
    Func<string, bool> fileExists,
    Func<string, string> readAllText) : IApplicationConfigurationLoader
{
    private static readonly HashSet<string> ThemeKeys =
    [
        "preset",
        "text",
        "accent",
        "heading",
        "border",
        "muted",
        "success",
        "warning",
        "error",
        "tag",
        "date",
        "background",
        "surface",
        "surface_2",
        "secondary_text",
        "border_active",
        "accent_bright",
        "info"
    ];

    private static readonly HashSet<string> GoogleCalendarKeys =
    [
        "enabled",
        "oauth_client_file"
    ];

    private static readonly IReadOnlyDictionary<string, Color> NamedColors = typeof(Color)
        .GetProperties(BindingFlags.Public | BindingFlags.Static)
        .Where(property => property.PropertyType == typeof(Color))
        .ToDictionary(
            property => property.Name,
            property => (Color)property.GetValue(null)!,
            StringComparer.OrdinalIgnoreCase);

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
        var theme = ReadTheme(document);
        var googleCalendar = ReadGoogleCalendar(document);
        return new ApplicationConfiguration(files, bindings)
        {
            Theme = theme,
            GoogleCalendar = googleCalendar
        };
    }

    private static GoogleCalendarConfiguration ReadGoogleCalendar(TomlTable document)
    {
        if (!document.TryGetValue("google_calendar", out var calendarValue))
        {
            return GoogleCalendarConfiguration.Disabled;
        }

        if (calendarValue is not TomlTable calendar)
        {
            throw new InvalidDataException("Invalid configuration file: google_calendar must be a TOML table.");
        }

        var unknownKey = calendar.Keys.FirstOrDefault(key => !GoogleCalendarKeys.Contains(key));
        if (unknownKey is not null)
        {
            throw new InvalidDataException(
                $"Invalid configuration file: google_calendar.{unknownKey} is not supported.");
        }

        var enabled = calendar.TryGetValue("enabled", out var enabledValue)
            ? enabledValue is bool value
                ? value
                : throw new InvalidDataException(
                    "Invalid configuration file: google_calendar.enabled must be true or false.")
            : false;
        var oauthClientFile = calendar.TryGetValue("oauth_client_file", out var clientFileValue)
            ? clientFileValue as string ?? throw new InvalidDataException(
                "Invalid configuration file: google_calendar.oauth_client_file must be a string.")
            : null;

        if (enabled && (string.IsNullOrWhiteSpace(oauthClientFile) || !Path.IsPathFullyQualified(oauthClientFile)))
        {
            throw new InvalidDataException(
                "Invalid configuration file: google_calendar.oauth_client_file must be an absolute path when enabled.");
        }

        return new GoogleCalendarConfiguration(enabled, oauthClientFile);
    }

    private static TuiTheme ReadTheme(TomlTable document)
    {
        if (!document.TryGetValue("tui", out var tuiValue))
        {
            return TuiThemes.Wolf;
        }

        if (tuiValue is not TomlTable tui)
        {
            throw new InvalidDataException("Invalid configuration file: tui must be a TOML table.");
        }

        if (!tui.TryGetValue("theme", out var themeValue))
        {
            return TuiThemes.Wolf;
        }

        if (themeValue is not TomlTable theme)
        {
            throw new InvalidDataException("Invalid configuration file: tui.theme must be a TOML table.");
        }

        var unknownKey = theme.Keys.FirstOrDefault(key => !ThemeKeys.Contains(key));
        if (unknownKey is not null)
        {
            throw new InvalidDataException(
                $"Invalid configuration file: tui.theme.{unknownKey} is not a supported theme setting.");
        }

        var preset = ReadPreset(theme);
        return preset with
        {
            Text = ReadThemeColor(theme, "text", preset.Text),
            Accent = ReadThemeColor(theme, "accent", preset.Accent),
            Heading = ReadThemeColor(theme, "heading", preset.Heading),
            Border = ReadThemeColor(theme, "border", preset.Border),
            Muted = ReadThemeColor(theme, "muted", preset.Muted),
            Success = ReadThemeColor(theme, "success", preset.Success),
            Warning = ReadThemeColor(theme, "warning", preset.Warning),
            Error = ReadThemeColor(theme, "error", preset.Error),
            Tag = ReadThemeColor(theme, "tag", preset.Tag),
            Date = ReadThemeColor(theme, "date", preset.Date),
            Background = ReadThemeColor(theme, "background", preset.Background),
            Surface = ReadThemeColor(theme, "surface", preset.Surface),
            Surface2 = ReadThemeColor(theme, "surface_2", preset.Surface2),
            SecondaryText = ReadThemeColor(theme, "secondary_text", preset.SecondaryText),
            BorderActive = ReadThemeColor(theme, "border_active", preset.BorderActive),
            AccentBright = ReadThemeColor(theme, "accent_bright", preset.AccentBright),
            Info = ReadThemeColor(theme, "info", preset.Info)
        };
    }

    private static TuiTheme ReadPreset(TomlTable theme)
    {
        if (!theme.TryGetValue("preset", out var presetValue))
        {
            return TuiThemes.Wolf;
        }

        if (presetValue is not string presetName ||
            string.IsNullOrWhiteSpace(presetName) ||
            !TuiThemes.TryGet(presetName, out var preset))
        {
            throw new InvalidDataException(
                "Invalid configuration file: tui.theme.preset must be one of wolf, classic, or mono.");
        }

        return preset;
    }

    private static Color ReadThemeColor(TomlTable theme, string name, Color defaultValue)
    {
        if (!theme.TryGetValue(name, out var value))
        {
            return defaultValue;
        }

        if (value is not string colorName || !TryParseColor(colorName, out var color))
        {
            throw new InvalidDataException(
                $"Invalid configuration file: tui.theme.{name} must be a named color, #RRGGBB, or default.");
        }

        return color;
    }

    private static bool TryParseColor(string value, out Color color)
    {
        if (string.Equals(value, "default", StringComparison.OrdinalIgnoreCase))
        {
            color = Color.Default;
            return true;
        }

        if (value.Length == 7 && value[0] == '#' && Color.TryFromHex(value, out color))
        {
            return true;
        }

        return NamedColors.TryGetValue(value, out color);
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
        var helpCommand = ReadOptionalCommand(keybindings, "help", defaults.HelpCommand);
        var result = defaults with
        {
            ToggleCompletedCommand = completedCommand,
            HelpCommand = helpCommand,
            MoveUp = ReadGestures(keybindings, "move_up", defaults.MoveUp),
            MoveDown = ReadGestures(keybindings, "move_down", defaults.MoveDown),
            JumpTop = ReadGestures(keybindings, "jump_top", defaults.JumpTop),
            JumpBottom = ReadGestures(keybindings, "jump_bottom", defaults.JumpBottom),
            FocusNext = ReadGestures(keybindings, "focus_next", defaults.FocusNext),
            FocusPrevious = ReadGestures(keybindings, "focus_previous", defaults.FocusPrevious),
            Open = ReadGestures(keybindings, "open", defaults.Open),
            Back = ReadGestures(keybindings, "back", defaults.Back),
            CommandMode = ReadGestures(keybindings, "command_mode", defaults.CommandMode),
            CommandPalette = ReadGestures(
                keybindings, "command_palette", defaults.CommandPalette),
            FilterMode = ReadGestures(keybindings, "filter_mode", defaults.FilterMode),
            SortMode = ReadGestures(keybindings, "sort_mode", defaults.SortMode),
            TabNext = ReadGestures(keybindings, "tab_next", defaults.TabNext),
            TabPrevious = ReadGestures(keybindings, "tab_previous", defaults.TabPrevious),
            PlannerPreviousDay = ReadGestures(
                keybindings, "planner_previous_day", defaults.PlannerPreviousDay),
            PlannerNextDay = ReadGestures(keybindings, "planner_next_day", defaults.PlannerNextDay),
            PlannerToday = ReadGestures(keybindings, "planner_today", defaults.PlannerToday),
            PlannerUnschedule = ReadGestures(
                keybindings, "planner_unschedule", defaults.PlannerUnschedule),
            PlannerRefreshCalendar = ReadGestures(
                keybindings, "planner_refresh_calendar", defaults.PlannerRefreshCalendar),
            CreateTodo = ReadGestures(keybindings, "create_todo", defaults.CreateTodo),
            EditTodo = ReadGestures(keybindings, "edit_todo", defaults.EditTodo),
            EditTodoContent = ReadGestures(
                keybindings, "edit_todo_content", defaults.EditTodoContent),
            EditTodoExternal = ReadGestures(
                keybindings, "edit_todo_external", defaults.EditTodoExternal),
            ToggleTodo = ReadGestures(keybindings, "toggle_todo", defaults.ToggleTodo),
            ToggleDetails = ReadGestures(
                keybindings, "toggle_details", defaults.ToggleDetails),
            RemoveContent = ReadGestures(
                keybindings, "remove_content", defaults.RemoveContent),
            SaveForm = ReadGestures(keybindings, "save_form", defaults.SaveForm)
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
        var commands = new[]
        {
            (Name: "quit", Value: bindings.QuitCommand),
            (Name: "toggle_completed", Value: bindings.ToggleCompletedCommand),
            (Name: "help", Value: bindings.HelpCommand)
        };
        var duplicate = commands.GroupBy(command => command.Value).FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidDataException(
                "Invalid configuration file: keybindings.quit, keybindings.toggle_completed, " +
                "and keybindings.help must be different.");
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
            ("command_palette", bindings.CommandPalette),
            ("filter_mode", bindings.FilterMode),
            ("sort_mode", bindings.SortMode),
            ("tab_next", bindings.TabNext),
            ("tab_previous", bindings.TabPrevious),
            ("planner_previous_day", bindings.PlannerPreviousDay),
            ("planner_next_day", bindings.PlannerNextDay),
            ("planner_today", bindings.PlannerToday),
            ("planner_unschedule", bindings.PlannerUnschedule),
            ("planner_refresh_calendar", bindings.PlannerRefreshCalendar),
            ("create_todo", bindings.CreateTodo),
            ("edit_todo", bindings.EditTodo),
            ("edit_todo_content", bindings.EditTodoContent),
            ("edit_todo_external", bindings.EditTodoExternal),
            ("toggle_todo", bindings.ToggleTodo),
            ("toggle_details", bindings.ToggleDetails),
            ("remove_content", bindings.RemoveContent),
            ("jump_top", bindings.JumpTop),
            ("jump_bottom", bindings.JumpBottom)
        };
        var owners = new Dictionary<KeyGesture, string>();

        foreach (var action in actions)
        {
            foreach (var gesture in action.Gestures)
            {
                if (owners.TryGetValue(gesture, out var owner))
                {
                    if (CanShareGesture(owner, action.Name))
                    {
                        continue;
                    }

                    throw new InvalidDataException(
                        $"Invalid configuration file: key gesture '{gesture.DisplayName}' is assigned to both " +
                        $"keybindings.{owner} and keybindings.{action.Name}.");
                }

                owners.Add(gesture, action.Name);
            }
        }
    }

    private static bool CanShareGesture(string first, string second) =>
        (first == "planner_today" && second == "jump_top") ||
        (first == "jump_top" && second == "planner_today");
}
