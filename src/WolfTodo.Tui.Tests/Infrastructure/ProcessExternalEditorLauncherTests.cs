using System.ComponentModel;
using System.Diagnostics;
using FluentAssertions;
using WolfTodo.Tui.Infrastructure;

namespace WolfTodo.Tui.Tests.Infrastructure;

public sealed class ProcessExternalEditorLauncherTests
{
    [Theory]
    [InlineData("hx", "/projects/work list.md:12")]
    [InlineData("/opt/homebrew/bin/helix", "/projects/work list.md:12")]
    [InlineData("nvim", "+12|/projects/work list.md")]
    [InlineData("vim", "+12|/projects/work list.md")]
    [InlineData("nano", "+12|/projects/work list.md")]
    [InlineData("custom-editor", "/projects/work list.md")]
    public void Open_builds_editor_specific_arguments(
        string editor,
        string expectedArguments)
    {
        ProcessStartInfo? captured = null;
        var launcher = new ProcessExternalEditorLauncher(
            () => editor,
            startInfo =>
            {
                captured = startInfo;
                return 0;
            });

        var result = launcher.Open("/projects/work list.md", 12);

        result.Error.Should().BeNull();
        result.Started.Should().BeTrue();
        captured!.FileName.Should().Be(editor);
        captured.UseShellExecute.Should().BeFalse();
        captured.ArgumentList.Should().Equal(expectedArguments.Split('|'));
    }

    [Fact]
    public void Open_reports_a_missing_editor_without_starting_a_process()
    {
        var invoked = false;
        var launcher = new ProcessExternalEditorLauncher(
            () => null,
            _ =>
            {
                invoked = true;
                return 0;
            });

        var result = launcher.Open("/projects/work.md", 1);

        result.Started.Should().BeFalse();
        result.Error.Should().Be("$EDITOR is not configured.");
        invoked.Should().BeFalse();
    }

    [Fact]
    public void Open_reports_nonzero_exits_and_launch_failures()
    {
        var exited = new ProcessExternalEditorLauncher(() => "hx", _ => 7)
            .Open("/projects/work.md", 1);
        var failed = new ProcessExternalEditorLauncher(
            () => "hx",
            _ => throw new Win32Exception("not found"))
            .Open("/projects/work.md", 1);

        exited.Started.Should().BeTrue();
        exited.Error.Should().Be("$EDITOR exited with code 7.");
        failed.Started.Should().BeFalse();
        failed.Error.Should().Contain("Unable to start $EDITOR").And.Contain("not found");
    }
}
