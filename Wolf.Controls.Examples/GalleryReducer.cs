using Wolf.Controls;
using Wolf.Controls.ProgressBar;
using Wolf.Controls.Splash;

namespace Wolf.Controls.Examples;

internal static class GalleryReducer
{
    private static readonly DemoId[] Demos = Enum.GetValues<DemoId>();

    public static GalleryTransition Reduce(GalleryState state, ConsoleKeyInfo key, DateTimeOffset now)
    {
        if (state.Mode == GalleryMode.FocusedControl)
        {
            return ReduceFocusedControl(state, key);
        }

        if (key.Key is ConsoleKey.Q or ConsoleKey.Escape)
        {
            return new(state, ExitRequested: true);
        }

        if (TryGetDemo(key, out var demo))
        {
            return new(state with { ActiveDemo = demo, Status = $"Viewing {Title(demo)}." });
        }

        if (key.Key is ConsoleKey.LeftArrow or ConsoleKey.RightArrow)
        {
            var offset = key.Key == ConsoleKey.LeftArrow ? -1 : 1;
            var index = (Array.IndexOf(Demos, state.ActiveDemo) + offset + Demos.Length) % Demos.Length;
            return new(state with { ActiveDemo = Demos[index], Status = $"Viewing {Title(Demos[index])}." });
        }

        if (key.Key == ConsoleKey.Enter && state.ActiveDemo == DemoId.TextBox)
        {
            return new(state with
            {
                Mode = GalleryMode.FocusedControl,
                TextBox = state.TextBox with { IsActive = true },
                TextBoxBeforeFocus = state.TextBox,
                Status = "Text box focused. Enter saves; Esc cancels."
            });
        }

        if (key.Key == ConsoleKey.Enter && state.ActiveDemo == DemoId.SelectList)
        {
            return new(state with
            {
                Mode = GalleryMode.FocusedControl,
                SelectListBeforeFocus = state.SelectList,
                Status = "Select list focused. Enter selects; Esc cancels."
            });
        }

        if (key.Key == ConsoleKey.P && state.ActiveDemo == DemoId.ProgressBar)
        {
            return new(state with
            {
                ProgressStartedAt = now,
                PreviousProgressValue = 0,
                ProgressValue = 0,
                ProgressChangedAt = now,
                Status = "Progress animation restarted."
            });
        }

        if (key.Key == ConsoleKey.T && state.ActiveDemo == DemoId.Toast)
        {
            return new(state with
            {
                Toast = new ToastState("Your controls are ready to use.", ToastSeverity.Information, now, now + TimeSpan.FromSeconds(3)),
                Status = "Toast triggered."
            });
        }

        if (key.Key == ConsoleKey.T && state.ActiveDemo == DemoId.Splash)
        {
            return new(state with
            {
                SplashBox = SplashBoxState.Create("Wolf Controls", "Press T to replay · Left/Right to browse", now),
                Status = "Splash expansion replayed."
            });
        }

        return new(state);
    }

    public static GalleryState Tick(GalleryState state, DateTimeOffset now)
    {
        var toast = state.Toast is { } currentToast && !currentToast.IsVisibleAt(now) ? null : state.Toast;
        if (state.ActiveDemo != DemoId.ProgressBar)
        {
            return state with { Toast = toast };
        }

        var elapsed = (now - state.ProgressStartedAt).TotalMilliseconds;
        var duration = TimeSpan.FromSeconds(3).TotalMilliseconds;
        var progress = Math.Clamp((elapsed % duration) / duration, 0, 1);
        return state with
        {
            PreviousProgressValue = ProgressBar.ProgressBar.ValueAt(
                new ProgressState("Downloading", state.PreviousProgressValue, state.ProgressValue, state.ProgressChangedAt), now),
            ProgressValue = progress,
            ProgressChangedAt = now,
            Toast = toast
        };
    }

    private static GalleryTransition ReduceFocusedControl(GalleryState state, ConsoleKeyInfo key)
    {
        if (state.ActiveDemo == DemoId.TextBox)
        {
            var transition = TextBox.Default.Reduce(state.TextBox, ConsoleInputMapper.ToControlInput(key));
            return transition.Outcome switch
            {
                TextBoxOutcome.Accepted => new(state with
                {
                    Mode = GalleryMode.Browse,
                    TextBox = transition.State! with { IsActive = false },
                    TextBoxBeforeFocus = null,
                    Status = $"Saved '{transition.State.Text}'."
                }),
                TextBoxOutcome.Cancelled => new(state with
                {
                    Mode = GalleryMode.Browse,
                    TextBox = state.TextBoxBeforeFocus! with { IsActive = false },
                    TextBoxBeforeFocus = null,
                    Status = "Text edit cancelled."
                }),
                _ => new(state with { TextBox = transition.State! })
            };
        }

        var selectTransition = SelectList.Default.Reduce(state.SelectList, ConsoleInputMapper.ToControlInput(key));
        if (selectTransition.Outcome == SelectListOutcome.Accepted)
        {
            var selectedState = selectTransition.State!;
            return new(state with
            {
                Mode = GalleryMode.Browse,
                SelectList = selectedState,
                SelectListBeforeFocus = null,
                Status = $"Selected '{selectedState.SelectedOption!.Label}'."
            });
        }

        return selectTransition.Outcome switch
        {
            SelectListOutcome.Cancelled => new(state with
            {
                Mode = GalleryMode.Browse,
                SelectList = state.SelectListBeforeFocus!,
                SelectListBeforeFocus = null,
                Status = "Selection cancelled."
            }),
            _ => new(state with { SelectList = selectTransition.State! })
        };
    }

    private static bool TryGetDemo(ConsoleKeyInfo key, out DemoId demo)
    {
        var index = key.KeyChar - '1';
        if (index < 0 || index >= Demos.Length)
        {
            demo = default;
            return false;
        }

        demo = Demos[index];
        return true;
    }

    private static string Title(DemoId demo) => demo switch
    {
        DemoId.TextBox => "Text box",
        DemoId.SelectList => "Select list",
        DemoId.ProgressBar => "Progress bar",
        DemoId.Splash => "Splash box",
        _ => demo.ToString()
    };
}
