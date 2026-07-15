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
    ImmutableArray<KeyGesture> SortMode,
    ImmutableArray<KeyGesture> TabNext,
    ImmutableArray<KeyGesture> TabPrevious,
    ImmutableArray<KeyGesture> PlannerPreviousDay,
    ImmutableArray<KeyGesture> PlannerNextDay,
    ImmutableArray<KeyGesture> PlannerToday,
    ImmutableArray<KeyGesture> PlannerUnschedule,
    ImmutableArray<KeyGesture> CreateTodo,
    ImmutableArray<KeyGesture> EditTodo,
    ImmutableArray<KeyGesture> ToggleTodo,
    ImmutableArray<KeyGesture> SaveForm)
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
        Gestures("t"),
        Gestures("L"),
        Gestures("H"),
        Gestures("["),
        Gestures("]"),
        Gestures("g"),
        Gestures("u"),
        Gestures("a"),
        Gestures("e"),
        Gestures("Spacebar"),
        Gestures("Ctrl+S"));

    public bool MatchesMoveUp(ConsoleKeyInfo key) => Matches(MoveUp, key);

    public bool MatchesMoveDown(ConsoleKeyInfo key) => Matches(MoveDown, key);

    public bool MatchesFocusNext(ConsoleKeyInfo key) => Matches(FocusNext, key);

    public bool MatchesFocusPrevious(ConsoleKeyInfo key) => Matches(FocusPrevious, key);

    public bool MatchesOpen(ConsoleKeyInfo key) => Matches(Open, key);

    public bool MatchesBack(ConsoleKeyInfo key) => Matches(Back, key);

    public bool MatchesCommandMode(ConsoleKeyInfo key) => Matches(CommandMode, key);

    public bool MatchesFilterMode(ConsoleKeyInfo key) => Matches(FilterMode, key);

    public bool MatchesSortMode(ConsoleKeyInfo key) => Matches(SortMode, key);

    public bool MatchesTabNext(ConsoleKeyInfo key) => Matches(TabNext, key);

    public bool MatchesTabPrevious(ConsoleKeyInfo key) => Matches(TabPrevious, key);

    public bool MatchesPlannerPreviousDay(ConsoleKeyInfo key) => Matches(PlannerPreviousDay, key);

    public bool MatchesPlannerNextDay(ConsoleKeyInfo key) => Matches(PlannerNextDay, key);

    public bool MatchesPlannerToday(ConsoleKeyInfo key) => Matches(PlannerToday, key);

    public bool MatchesPlannerUnschedule(ConsoleKeyInfo key) => Matches(PlannerUnschedule, key);

    public bool MatchesCreateTodo(ConsoleKeyInfo key) => Matches(CreateTodo, key);

    public bool MatchesEditTodo(ConsoleKeyInfo key) => Matches(EditTodo, key);

    public bool MatchesToggleTodo(ConsoleKeyInfo key) => Matches(ToggleTodo, key);

    public bool MatchesSaveForm(ConsoleKeyInfo key) => Matches(SaveForm, key);

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
