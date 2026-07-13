namespace WolfTodo.Tui.Features.Configuration;

public readonly record struct KeyGesture
{
    private KeyGesture(char? character, ConsoleKey? key, ConsoleModifiers modifiers)
    {
        Character = character;
        Key = key;
        Modifiers = modifiers;
    }

    public char? Character { get; }

    public ConsoleKey? Key { get; }

    public ConsoleModifiers Modifiers { get; }

    public string DisplayName
    {
        get
        {
            if (Character is not null)
            {
                return Character.Value.ToString();
            }

            var parts = new List<string>();

            if (Modifiers.HasFlag(ConsoleModifiers.Control))
            {
                parts.Add("Ctrl");
            }

            if (Modifiers.HasFlag(ConsoleModifiers.Alt))
            {
                parts.Add("Alt");
            }

            if (Modifiers.HasFlag(ConsoleModifiers.Shift))
            {
                parts.Add("Shift");
            }

            parts.Add(Key!.Value.ToString());
            return string.Join('+', parts);
        }
    }

    public bool Matches(ConsoleKeyInfo input) => Character is not null
        ? input.KeyChar == Character.Value
        : input.Key == Key && input.Modifiers == Modifiers;

    public static KeyGesture Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new FormatException("A key gesture must be a non-empty string.");
        }

        if (value.Length == 1 && !char.IsControl(value[0]))
        {
            return new KeyGesture(value[0], null, ConsoleModifiers.None);
        }

        var parts = value.Split('+', StringSplitOptions.TrimEntries);

        if (parts.Any(string.IsNullOrEmpty))
        {
            throw new FormatException($"Invalid key gesture '{value}'.");
        }

        var modifiers = ConsoleModifiers.None;

        foreach (var part in parts[..^1])
        {
            var modifier = part.ToUpperInvariant() switch
            {
                "CTRL" or "CONTROL" => ConsoleModifiers.Control,
                "ALT" => ConsoleModifiers.Alt,
                "SHIFT" => ConsoleModifiers.Shift,
                _ => throw new FormatException($"Invalid modifier '{part}' in key gesture '{value}'.")
            };

            if (modifiers.HasFlag(modifier))
            {
                throw new FormatException($"Duplicate modifier '{part}' in key gesture '{value}'.");
            }

            modifiers |= modifier;
        }

        if (!Enum.TryParse<ConsoleKey>(parts[^1], true, out var key) || !Enum.IsDefined(key))
        {
            throw new FormatException($"Invalid console key '{parts[^1]}' in key gesture '{value}'.");
        }

        return new KeyGesture(null, key, modifiers);
    }
}
