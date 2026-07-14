using System.Collections.Immutable;

namespace WolfTodo.Tui.Features.Configuration;

public sealed record TuiKeyBindings(
    string QuitCommand,
    string ToggleCompletedCommand,
    ImmutableArray<KeyGesture> MoveUp,
    ImmutableArray<KeyGesture> MoveDown,
    ImmutableArray<KeyGesture> FocusNext,
    ImmutableArray<KeyGesture> FocusPrevious,
    ImmutableArray<KeyGesture> Open,
    ImmutableArray<KeyGesture> Back,
    ImmutableArray<KeyGesture> CommandMode,
    ImmutableArray<KeyGesture> FilterMode,
    ImmutableArray<KeyGesture> TabNext,
    ImmutableArray<KeyGesture> TabPrevious)
{
    public static TuiKeyBindings CreateDefaults(string quitCommand) => new(
        quitCommand,
        ":completed",
        Gestures("UpArrow", "k"),
        Gestures("DownArrow", "j"),
        Gestures("Tab"),
        Gestures("Shift+Tab"),
        Gestures("Enter", "l"),
        Gestures("Escape", "h"),
        Gestures(":"),
        Gestures("/"),
        Gestures("Ctrl+Tab"),
        Gestures("Ctrl+Shift+Tab"));

    public bool MatchesMoveUp(ConsoleKeyInfo key) => Matches(MoveUp, key);

    public bool MatchesMoveDown(ConsoleKeyInfo key) => Matches(MoveDown, key);

    public bool MatchesFocusNext(ConsoleKeyInfo key) => Matches(FocusNext, key);

    public bool MatchesFocusPrevious(ConsoleKeyInfo key) => Matches(FocusPrevious, key);

    public bool MatchesOpen(ConsoleKeyInfo key) => Matches(Open, key);

    public bool MatchesBack(ConsoleKeyInfo key) => Matches(Back, key);

    public bool MatchesCommandMode(ConsoleKeyInfo key) => Matches(CommandMode, key);

    public bool MatchesFilterMode(ConsoleKeyInfo key) => Matches(FilterMode, key);

    public bool MatchesTabNext(ConsoleKeyInfo key) => Matches(TabNext, key);

    public bool MatchesTabPrevious(ConsoleKeyInfo key) => Matches(TabPrevious, key);

    public static string ShortestDisplayName(ImmutableArray<KeyGesture> gestures) => gestures
        .Select((gesture, index) => (gesture.DisplayName, Index: index))
        .OrderBy(candidate => candidate.DisplayName.Length)
        .ThenBy(candidate => candidate.Index)
        .First().DisplayName;

    private static bool Matches(ImmutableArray<KeyGesture> gestures, ConsoleKeyInfo key) =>
        gestures.Any(gesture => gesture.Matches(key));

    private static ImmutableArray<KeyGesture> Gestures(params string[] values) =>
        [.. values.Select(KeyGesture.Parse)];
}
