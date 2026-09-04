using System.Diagnostics;
using FluentAssertions;
using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Infrastructure;
using WolfTodo.Tui.Infrastructure.Notifications;

namespace WolfTodo.Tui.Tests.Infrastructure;

public sealed class PlatformPomodoroCompletionNotifierTests
{
    [Fact]
    public void Notify_writes_a_sanitized_desktop_notification_and_starts_sound()
    {
        var output = new List<string>();
        ProcessStartInfo? startInfo = null;
        var notifier = new PlatformPomodoroCompletionNotifier(
            output.Add,
            info =>
            {
                startInfo = info;
                return true;
            });

        notifier.Notify(new PomodoroCompletion("Deep\a work", TimeSpan.FromMinutes(25), DateTime.Now), true);

        output.Should().ContainSingle().Which.Should().Contain("\u001b]9;")
            .And.Contain("Wolf Todo — Pomodoro complete: Deep work")
            .And.EndWith("\a");
        startInfo.Should().NotBeNull();
        startInfo!.UseShellExecute.Should().BeFalse();
    }

    [Fact]
    public void Notify_falls_back_to_bell_when_native_sound_cannot_start()
    {
        var output = new List<string>();
        var notifier = new PlatformPomodoroCompletionNotifier(output.Add, _ => false);

        notifier.Notify(new PomodoroCompletion(null, TimeSpan.FromMinutes(25), DateTime.Now), true);

        output.Should().Contain("\a");
    }

    [Fact]
    public void Notify_keeps_sound_silent_when_disabled()
    {
        var output = new List<string>();
        var started = false;
        var notifier = new PlatformPomodoroCompletionNotifier(
            output.Add,
            _ =>
            {
                started = true;
                return true;
            });

        notifier.Notify(new PomodoroCompletion(null, TimeSpan.FromMinutes(25), DateTime.Now), false);

        started.Should().BeFalse();
        output.Should().ContainSingle();
    }
}
