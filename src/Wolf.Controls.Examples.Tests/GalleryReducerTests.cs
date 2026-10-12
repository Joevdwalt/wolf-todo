using FluentAssertions;
using Wolf.Controls.Examples;

namespace Wolf.Controls.Examples.Tests;

public sealed class GalleryReducerTests
{
    [Fact]
    public void Reduce_selects_numbered_demos_and_wraps_arrow_navigation()
    {
        var now = DateTimeOffset.UnixEpoch;
        var state = GalleryState.Create(now);

        var numbered = GalleryReducer.Reduce(state, Key('4', ConsoleKey.D4), now).State;
        var wrapped = GalleryReducer.Reduce(state, Key(ConsoleKey.LeftArrow), now).State;

        numbered.ActiveDemo.Should().Be(DemoId.ProgressBar);
        wrapped.ActiveDemo.Should().Be(DemoId.MultilineTextBox);
    }

    [Fact]
    public void Reduce_focuses_textbox_and_cancel_restores_the_original_value()
    {
        var now = DateTimeOffset.UnixEpoch;
        var state = GalleryState.Create(now);
        state = GalleryReducer.Reduce(state, Key(ConsoleKey.Enter), now).State;
        state = GalleryReducer.Reduce(state, Key('!', ConsoleKey.D1), now).State;

        var cancelled = GalleryReducer.Reduce(state, Key(ConsoleKey.Escape), now).State;

        cancelled.Mode.Should().Be(GalleryMode.Browse);
        cancelled.TextBox.Text.Should().Be("Wolf Controls");
        cancelled.Status.Should().Be("Text edit cancelled.");
    }

    [Fact]
    public void Reduce_triggers_the_active_feedback_demo()
    {
        var now = DateTimeOffset.UnixEpoch;
        var progress = GalleryReducer.Reduce(GalleryState.Create(now), Key('4', ConsoleKey.D4), now).State;
        var restarted = GalleryReducer.Reduce(progress, Key('p', ConsoleKey.P), now + TimeSpan.FromSeconds(1)).State;
        var toast = GalleryReducer.Reduce(GalleryState.Create(now), Key('5', ConsoleKey.D5), now).State;
        var triggered = GalleryReducer.Reduce(toast, Key('t', ConsoleKey.T), now).State;

        restarted.ProgressStartedAt.Should().Be(now + TimeSpan.FromSeconds(1));
        triggered.Toast.Should().NotBeNull();
        triggered.Toast!.VisibleUntil.Should().Be(now + TimeSpan.FromSeconds(3));
    }

    [Fact]
    public void Reduce_selects_and_replays_the_splash_demo()
    {
        var now = DateTimeOffset.UnixEpoch;
        var splash = GalleryReducer.Reduce(GalleryState.Create(now), Key('6', ConsoleKey.D6), now).State;
        var replayed = GalleryReducer.Reduce(splash, Key('t', ConsoleKey.T), now + TimeSpan.FromSeconds(1)).State;

        splash.ActiveDemo.Should().Be(DemoId.Splash);
        replayed.SplashBox!.StartedAt.Should().Be(now + TimeSpan.FromSeconds(1));
        replayed.Status.Should().Be("Splash expansion replayed.");
    }

    [Fact]
    public void Reduce_edits_multiline_text_and_saves_with_control_s()
    {
        var now = DateTimeOffset.UnixEpoch;
        var state = GalleryReducer.Reduce(GalleryState.Create(now), Key('7', ConsoleKey.D7), now).State;
        state = GalleryReducer.Reduce(state, Key(ConsoleKey.Enter), now).State;
        state = GalleryReducer.Reduce(state, Key(ConsoleKey.Enter), now).State;

        var saved = GalleryReducer.Reduce(state, new ConsoleKeyInfo('\0', ConsoleKey.S, false, false, true), now).State;

        saved.Mode.Should().Be(GalleryMode.Browse);
        saved.MultilineTextBox.Text.Should().EndWith("\n");
    }

    private static ConsoleKeyInfo Key(ConsoleKey key) => new('\0', key, false, false, false);

    private static ConsoleKeyInfo Key(char character, ConsoleKey key) => new(character, key, false, false, false);
}
