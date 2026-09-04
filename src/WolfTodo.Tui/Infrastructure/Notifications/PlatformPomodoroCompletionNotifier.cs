using System.Diagnostics;
using WolfTodo.Tui.Features.ApplicationShell;

namespace WolfTodo.Tui.Infrastructure.Notifications;

public sealed class PlatformPomodoroCompletionNotifier : IPomodoroCompletionNotifier
{
    private readonly Action<string> write;
    private readonly Func<ProcessStartInfo, bool> startProcess;

    public PlatformPomodoroCompletionNotifier()
        : this(Write, StartProcess)
    {
    }

    public PlatformPomodoroCompletionNotifier(Action<string> write, Func<ProcessStartInfo, bool> startProcess)
    {
        this.write = write;
        this.startProcess = startProcess;
    }

    public void Notify(PomodoroCompletion completion, bool playSound)
    {
        try
        {
            write($"\u001b]9;{Sanitize("Wolf Todo — " + completion.NotificationBody)}\a");
        }
        catch (IOException)
        {
            // A terminal notification is optional; the in-app banner remains visible.
        }

        if (!playSound)
        {
            return;
        }

        try
        {
            if (!SoundStartInfos().Any(startProcess))
            {
                Bell();
            }
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            Bell();
        }
    }

    private static IEnumerable<ProcessStartInfo> SoundStartInfos()
    {
        if (OperatingSystem.IsMacOS())
        {
            var info = CreateStartInfo("/usr/bin/afplay");
            info.ArgumentList.Add("/System/Library/Sounds/Glass.aiff");
            yield return info;
        }
        else if (OperatingSystem.IsWindows())
        {
            var info = CreateStartInfo("powershell.exe");
            info.ArgumentList.Add("-NoProfile");
            info.ArgumentList.Add("-Command");
            info.ArgumentList.Add("[console]::Beep(880,250)");
            yield return info;
        }
        else
        {
            var canberra = CreateStartInfo("canberra-gtk-play");
            canberra.ArgumentList.Add("-i");
            canberra.ArgumentList.Add("complete");
            yield return canberra;

            var pulse = CreateStartInfo("paplay");
            pulse.ArgumentList.Add("/usr/share/sounds/freedesktop/stereo/complete.oga");
            yield return pulse;

            var alsa = CreateStartInfo("aplay");
            alsa.ArgumentList.Add("/usr/share/sounds/alsa/Front_Center.wav");
            yield return alsa;
        }
    }

    private static ProcessStartInfo CreateStartInfo(string fileName) => new()
    {
        FileName = fileName,
        UseShellExecute = false,
        CreateNoWindow = true
    };

    private void Bell()
    {
        try { write("\a"); } catch (IOException) { }
    }

    private static void Write(string value)
    {
        Console.Out.Write(value);
        Console.Out.Flush();
    }

    private static bool StartProcess(ProcessStartInfo info) => System.Diagnostics.Process.Start(info) is not null;

    private static string Sanitize(string value) => new string(value
        .Where(character => !char.IsControl(character) && character is not '\u001b' and not '\a')
        .Take(200)
        .ToArray());
}
