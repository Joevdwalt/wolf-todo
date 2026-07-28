using WolfTodo.Tui.Features.Configuration;

namespace WolfTodo.Tui.Features.DayPlanner;

public sealed class DayScheduleExportService(
    DayScheduleMarkdownRenderer renderer,
    IDayScheduleMarkdownFileStore fileStore)
{
    public DayScheduleExportResult Export(PlannerView view, DayScheduleExportConfiguration? configuration)
    {
        if (configuration is null)
        {
            return DayScheduleExportResult.Failure(
                "Day schedule export requires a [planner.export] configuration.");
        }

        try
        {
            var path = DayScheduleMarkdownPath.Create(view.State.SelectedDate, configuration);
            var contents = fileStore.FileExists(path) ? fileStore.ReadAllText(path) : string.Empty;
            var section = renderer.Render(view, configuration);
            fileStore.WriteAllTextAtomically(
                path,
                DayScheduleMarkdownDocument.ReplaceDaySection(contents, view.State.SelectedDate, section));
            return DayScheduleExportResult.Success(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return DayScheduleExportResult.Failure($"Could not export day schedule: {exception.Message}");
        }
    }
}
