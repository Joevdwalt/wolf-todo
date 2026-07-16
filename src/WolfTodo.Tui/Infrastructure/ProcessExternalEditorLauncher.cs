using System.ComponentModel;
using System.Diagnostics;
using WolfTodo.Tui.Features.ApplicationShell;

namespace WolfTodo.Tui.Infrastructure;

public sealed class ProcessExternalEditorLauncher : IExternalEditorLauncher
{
    private readonly Func<string?> readEditor;
    private readonly Func<ProcessStartInfo, int> runProcess;

    public ProcessExternalEditorLauncher()
        : this(
            () => Environment.GetEnvironmentVariable("EDITOR"),
            RunProcess)
    {
    }

    public ProcessExternalEditorLauncher(
        Func<string?> readEditor,
        Func<ProcessStartInfo, int> runProcess)
    {
        this.readEditor = readEditor;
        this.runProcess = runProcess;
    }

    public ExternalEditorResult Open(string projectPath, int sourceLine)
    {
        var editor = readEditor()?.Trim();
        if (string.IsNullOrEmpty(editor))
        {
            return ExternalEditorResult.Failure(false, "$EDITOR is not configured.");
        }

        var startInfo = CreateStartInfo(editor, projectPath, sourceLine);
        try
        {
            var exitCode = runProcess(startInfo);
            return exitCode == 0
                ? ExternalEditorResult.Success
                : ExternalEditorResult.Failure(true, $"$EDITOR exited with code {exitCode}.");
        }
        catch (Exception exception) when (
            exception is Win32Exception or InvalidOperationException or IOException)
        {
            return ExternalEditorResult.Failure(false, $"Unable to start $EDITOR: {exception.Message}");
        }
    }

    private static ProcessStartInfo CreateStartInfo(
        string editor,
        string projectPath,
        int sourceLine)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = editor,
            UseShellExecute = false
        };
        var name = Path.GetFileNameWithoutExtension(editor).ToLowerInvariant();
        switch (name)
        {
            case "hx":
            case "helix":
                startInfo.ArgumentList.Add($"{projectPath}:{sourceLine}");
                break;
            case "vi":
            case "vim":
            case "nvim":
            case "view":
            case "nano":
                startInfo.ArgumentList.Add($"+{sourceLine}");
                startInfo.ArgumentList.Add(projectPath);
                break;
            default:
                startInfo.ArgumentList.Add(projectPath);
                break;
        }

        return startInfo;
    }

    private static int RunProcess(ProcessStartInfo startInfo)
    {
        using var process = Process.Start(startInfo) ??
            throw new InvalidOperationException("The editor process did not start.");
        process.WaitForExit();
        return process.ExitCode;
    }
}
