using Wolf.Controls;
using Wolf.Controls.Splash;

namespace Wolf.Controls.Examples;

internal sealed record GalleryState(
    DemoId ActiveDemo,
    GalleryMode Mode,
    TextBoxState TextBox,
    TextBoxState? TextBoxBeforeFocus,
    SelectListState SelectList,
    SelectListState? SelectListBeforeFocus,
    DateTimeOffset ProgressStartedAt,
    double PreviousProgressValue,
    double ProgressValue,
    DateTimeOffset ProgressChangedAt,
    ToastState? Toast,
    SplashBoxState? SplashBox,
    string Status)
{
    public static GalleryState Create(DateTimeOffset now) => new(
        DemoId.TextBox,
        GalleryMode.Browse,
        TextBoxState.Create("Project name", isEditable: true, "Wolf Controls"),
        null,
        new SelectListState(
            "Choose a priority",
            [new SelectOption("High", "Needs attention"), new SelectOption("Medium", "Normal pace"), new SelectOption("Low", "Can wait")],
            0,
            null,
            "No options available.",
            "Up/Down MOVE  Enter SELECT  Esc CANCEL"),
        null,
        now,
        0,
        0,
        now,
        null,
        SplashBoxState.Create("Wolf Controls", "Press T to replay · Left/Right to browse", now),
        "Use Left/Right or 1-6 to select a control.");
}
