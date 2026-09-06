using Spectre.Console;
using Spectre.Console.Rendering;
using Wolf.Controls;

namespace Wolf.Controls.Examples;

internal static class GalleryRenderer
{
    public static IRenderable Render(GalleryState state, DateTimeOffset now, int width)
    {
        var constraints = new ControlConstraints(Math.Max(24, width - 8), 6);
        var content = new Rows(
            new Text("WOLF.CONTROLS // INTERACTIVE GALLERY", new Style(ControlTheme.Default.Text, decoration: Decoration.Bold)),
            new Text(Navigation(state), new Style(ControlTheme.Default.Muted, decoration: Decoration.Dim)),
            new Text(string.Empty),
            RenderActiveDemo(state, now, constraints),
            new Text(string.Empty),
            new Text(state.Status, new Style(ControlTheme.Default.Muted, decoration: Decoration.Dim)),
            new Text(Footer(state), new Style(ControlTheme.Default.Muted, decoration: Decoration.Dim)));
        return new Align(content, HorizontalAlignment.Center, VerticalAlignment.Middle);
    }

    public static int SafeWidth()
    {
        try
        {
            return Console.WindowWidth;
        }
        catch (IOException)
        {
            return 80;
        }
    }

    private static IRenderable RenderActiveDemo(GalleryState state, DateTimeOffset now, ControlConstraints constraints) => state.ActiveDemo switch
    {
        DemoId.TextBox => TextBox.Default.Render(state.TextBox, ControlTheme.Default, constraints),
        DemoId.SelectList => SelectList.Default.Render(state.SelectList, ControlTheme.Default, constraints),
        DemoId.Spinner => Spinner.Default.Render(new SpinnerState("Loading controls"), ControlTheme.Default, constraints, now),
        DemoId.ProgressBar => ProgressBar.Default.Render(
            new ProgressState("Downloading", state.PreviousProgressValue, state.ProgressValue, state.ProgressChangedAt),
            ControlTheme.Default,
            constraints,
            now),
        DemoId.Toast => state.Toast is null
            ? new Text("Press T to show a transient notification.", new Style(ControlTheme.Default.Text))
            : Toast.Default.Render(state.Toast, ControlTheme.Default, constraints, now),
        _ => new Text(string.Empty)
    };

    private static string Navigation(GalleryState state) =>
        $"[{(state.ActiveDemo == DemoId.TextBox ? "TEXT BOX" : "1 Text box")}]  " +
        $"[{(state.ActiveDemo == DemoId.SelectList ? "SELECT LIST" : "2 Select list")}]  " +
        $"[{(state.ActiveDemo == DemoId.Spinner ? "SPINNER" : "3 Spinner")}]  " +
        $"[{(state.ActiveDemo == DemoId.ProgressBar ? "PROGRESS" : "4 Progress")}]  " +
        $"[{(state.ActiveDemo == DemoId.Toast ? "TOAST" : "5 Toast")}]";

    private static string Footer(GalleryState state) => state.Mode == GalleryMode.FocusedControl
        ? "CONTROL FOCUSED // Enter ACCEPT // Esc CANCEL"
        : state.ActiveDemo switch
        {
            DemoId.TextBox or DemoId.SelectList => "Left/Right BROWSE // Enter INTERACT // Q EXIT",
            DemoId.ProgressBar => "Left/Right BROWSE // P RESTART // Q EXIT",
            DemoId.Toast => "Left/Right BROWSE // T TRIGGER // Q EXIT",
            _ => "Left/Right BROWSE // Q EXIT"
        };
}
