using Spectre.Console;

namespace WolfTodo.Tui.Features.Configuration;

public static class TuiThemes
{
    public static TuiTheme Wolf { get; } = new(
        new Color(216, 225, 232),
        new Color(242, 140, 40),
        new Color(255, 177, 74),
        new Color(35, 55, 74),
        new Color(107, 124, 142),
        new Color(108, 191, 132),
        new Color(226, 182, 77),
        new Color(217, 108, 108),
        new Color(108, 191, 132),
        new Color(95, 168, 211),
        new Color(9, 18, 27),
        new Color(16, 28, 40),
        new Color(22, 36, 51),
        new Color(162, 178, 193),
        new Color(53, 82, 107),
        new Color(255, 177, 74),
        new Color(95, 168, 211));

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
        Color.Default,
        Color.Default,
        Color.Default,
        Color.Default,
        Color.Default,
        Color.Default,
        Color.Cyan,
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
