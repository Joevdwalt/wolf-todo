using Spectre.Console;
using Spectre.Console.Rendering;
using Wolf.Controls;
using Wolf.Controls.ProgressBar;
using Wolf.Controls.Spinner;
using Wolf.Controls.Splash;
using Wolf.Controls.FormDialog;
using Wolf.Controls.TextPrompt;
using Wolf.Controls.Rendering;

namespace Wolf.Controls.Examples;

internal static class GalleryRenderer
{
    public static IRenderable Render(GalleryState state, DateTimeOffset now, int width, int height)
    {
        if (state.ActiveDemo == DemoId.Splash)
        {
            // Spectre's live display reserves the final terminal row for its cursor.
            // Rendering into that row causes Crop mode to retain only the last line.
            var viewportHeight = Math.Max(1, height - 1);
            return SplashBox.Default.Render(
                state.SplashBox ?? SplashBoxState.Create("Wolf Controls", "Press T to replay · Left/Right to browse", now),
                ControlTheme.Default,
                new ControlConstraints(width, viewportHeight),
                now);
        }

        var constraints = new ControlConstraints(Math.Max(24, width - 8), 6);
        var content = new Rows(
            new Text("WOLF.CONTROLS // INTERACTIVE GALLERY", new Style(ControlTheme.Default.Text, decoration: Decoration.Bold)),
            new Text(Navigation(state, width), new Style(ControlTheme.Default.Muted, decoration: Decoration.Dim)),
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

    public static int SafeHeight()
    {
        try
        {
            return Console.WindowHeight;
        }
        catch (IOException)
        {
            return 24;
        }
    }

    private static IRenderable RenderActiveDemo(GalleryState state, DateTimeOffset now, ControlConstraints constraints) => state.ActiveDemo switch
    {
        DemoId.TextBox => TextBox.TextBox.Default.Render(state.TextBox, ControlTheme.Default, constraints),
        DemoId.SelectList => SelectList.SelectList.Default.Render(state.SelectList, ControlTheme.Default, constraints),
        DemoId.MultilineTextBox => MultilineTextBox.MultilineTextBox.Default.Render(state.MultilineTextBox, ControlTheme.Default, constraints),
        DemoId.FormDialog => FormDialog.FormDialog.Render(
            new FormDialogView(
                [new FormDialogLine("EDIT RECORD", FormDialogRole.Label),
                    new FormDialogLine("Reference: EXAMPLE-42", FormDialogRole.Value),
                    new FormDialogLine("Enter SAVE · Esc CANCEL", FormDialogRole.Hint)],
                [TextBox.TextBoxState.Create("Title", true, "Review the plan", true)],
                constraints.ClampedWidth),
            ControlTheme.Default),
        DemoId.TextPrompt => TextPrompt.TextPrompt.Render(
            new TextPromptView(
                TextBox.TextBoxState.Create("Search term", true, "example", true),
                "Enter SEARCH · Esc CANCEL",
                "Search all records"),
            ControlTheme.Default,
            constraints),
        DemoId.Spinner => Spinner.Spinner.Default.Render(new SpinnerState("Loading controls"), ControlTheme.Default, constraints, now),
        DemoId.ProgressBar => ProgressBar.ProgressBar.Default.Render(
            new ProgressState("Downloading", state.PreviousProgressValue, state.ProgressValue, state.ProgressChangedAt),
            ControlTheme.Default,
            constraints,
            now),
        DemoId.Toast => RenderToastDemo(state, now, constraints),
        DemoId.Layouts => RenderLayoutsDemo(constraints),
        _ => new Text(string.Empty),
    };

    private static IRenderable RenderToastDemo(GalleryState state, DateTimeOffset now, ControlConstraints constraints)
    {
        var background = new Text("Notifications appear above the current view.",
            new Style(ControlTheme.Default.Text));
        if (state.Toast is null || !state.Toast.IsVisibleAt(now))
        {
            return background;
        }

        return new LayeredRenderable(constraints.ClampedWidth, 3,
        [
            new ControlLayer(background, 0, 1),
            new ControlLayer(Toast.Toast.Default.Render(state.Toast, ControlTheme.Default, constraints, now), 0, 0, 1)
        ]);
    }

    private static IRenderable RenderLayoutsDemo(ControlConstraints constraints)
    {
        var width = constraints.ClampedWidth;
        var height = 12;
        var workspaceHeight = height - 5;
        var workspace = new RowLayout(workspaceHeight,
            new LayoutChild(new Panel(new Text("PROJECTS\nAll\nWork\nPersonal")) { Border = BoxBorder.Rounded }, LayoutSize.Fixed(width / 3)),
            new LayoutChild(new Panel(new Text("TASKS\nReview the plan\nUpdate the notes\nPrepare the release")) { Border = BoxBorder.Rounded }, LayoutSize.Weighted(2)));
        var popupWidth = Math.Max(16, width - 6);
        var popup = new Panel(new Rows(
            new Text("EDIT TASK", new Style(ControlTheme.Default.Text, decoration: Decoration.Bold)),
            new Text("Title:  Review the plan"),
            new Text("Content: Add a short summary"),
            new Text("Enter SAVE  ·  Esc CANCEL", new Style(ControlTheme.Default.Muted))))
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(ControlTheme.Default.Accent),
            Width = popupWidth
        };
        var layers = new List<ControlLayer> { new(workspace, 0, 0) };
        layers.Add(OverlayLayout.Place(popup, width, workspaceHeight,
            OverlayAlignment.Bottom, width: popupWidth, height: 6, bottom: 0, zIndex: 10));
        var overlaidWorkspace = new LayeredRenderable(width, workspaceHeight, layers);
        return new ColumnLayout(height,
            new LayoutChild(new Panel(new Text("HEADER  //  TODOS · PLANNER · FOCUS")) { Border = BoxBorder.Square }, LayoutSize.Fixed(3)),
            new LayoutChild(overlaidWorkspace, LayoutSize.Weighted()),
            new LayoutChild(new Panel(new Text("STATUS  ·  Editor hints and errors remain here")) { Border = BoxBorder.Square }, LayoutSize.Fixed(2)));
    }

    private static string Navigation(GalleryState state, int width) => width < 100
        ? $"[{(int)state.ActiveDemo + 1}/10 {state.ActiveDemo}] · Left/Right BROWSE · 1-9,0 JUMP"
        :
        $"[{(state.ActiveDemo == DemoId.TextBox ? "TEXT BOX" : "1 Text box")}]  " +
        $"[{(state.ActiveDemo == DemoId.SelectList ? "SELECT LIST" : "2 Select list")}]  " +
        $"[{(state.ActiveDemo == DemoId.Spinner ? "SPINNER" : "3 Spinner")}]  " +
        $"[{(state.ActiveDemo == DemoId.ProgressBar ? "PROGRESS" : "4 Progress")}]  " +
        $"[{(state.ActiveDemo == DemoId.Toast ? "TOAST" : "5 Toast")}]\n" +
        $"[{(state.ActiveDemo == DemoId.Splash ? "SPLASH" : "6 Splash")}]  " +
        $"[{(state.ActiveDemo == DemoId.MultilineTextBox ? "MULTILINE" : "7 Multiline")}]  " +
        $"[{(state.ActiveDemo == DemoId.FormDialog ? "FORM DIALOG" : "8 Form dialog")}]  " +
        $"[{(state.ActiveDemo == DemoId.TextPrompt ? "TEXT PROMPT" : "9 Text prompt")}]  " +
        $"[{(state.ActiveDemo == DemoId.Layouts ? "LAYOUTS" : "0 Layouts")}]";

    private static string Footer(GalleryState state) => state.Mode == GalleryMode.FocusedControl
        ? state.ActiveDemo == DemoId.MultilineTextBox
            ? "CONTROL FOCUSED // Enter NEWLINE // Ctrl+S SAVE // Esc CANCEL"
            : "CONTROL FOCUSED // Enter ACCEPT // Esc CANCEL"
        : state.ActiveDemo switch
        {
            DemoId.TextBox or DemoId.SelectList or DemoId.MultilineTextBox => "Left/Right BROWSE // Enter INTERACT // Q EXIT",
            DemoId.ProgressBar => "Left/Right BROWSE // P RESTART // Q EXIT",
            DemoId.Toast => "Left/Right BROWSE // T TRIGGER // Q EXIT",
            DemoId.Layouts => "ROW/COLUMN SIZING // BOTTOM OVERLAY // Left/Right BROWSE // Q EXIT",
            DemoId.Splash => "T REPLAY // Left/Right BROWSE // Q EXIT",
            _ => "Left/Right BROWSE // Q EXIT"
        };
}
