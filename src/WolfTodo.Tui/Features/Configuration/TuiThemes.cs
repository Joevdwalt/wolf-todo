using Spectre.Console;

namespace WolfTodo.Tui.Features.Configuration;

public static class TuiThemes
{
    public static TuiTheme Wolf { get; } = new(
        Color.Default,
        new Color(95, 215, 255),
        new Color(175, 135, 255),
        new Color(95, 95, 135),
        new Color(128, 128, 128),
        new Color(95, 215, 135),
        new Color(255, 215, 95),
        new Color(255, 95, 95),
        new Color(95, 215, 175),
        new Color(135, 175, 255));

    public static TuiTheme Classic { get; } = new(
        Color.Default,
        Color.Cyan,
        Color.Default,
        Color.Default,
        Color.Default,
        Color.Default,
        Color.Default,
        Color.Red,
        Color.Default,
        Color.Default);

    public static TuiTheme Mono { get; } = new(
        Color.Default,
        Color.Default,
        Color.Default,
        Color.Default,
        Color.Default,
        Color.Default,
        Color.Default,
        Color.Default,
        Color.Default,
        Color.Default);

    public static bool TryGet(string name, out TuiTheme theme)
    {
        theme = name.ToLowerInvariant() switch
        {
            "wolf" => Wolf,
            "classic" => Classic,
            "mono" => Mono,
            _ => null!
        };

        return theme is not null;
    }
}
