using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Core.Infrastructure.Markdown;
using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Features.ApplicationShell.Rendering;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner.Rendering;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ProjectBrowser.Rendering;
using WolfTodo.Tui.Features.Splash;
using WolfTodo.Tui.Features.Tabs;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Infrastructure;
using WolfTodo.Tui.Infrastructure.Calendar;
using WolfTodo.Tui.Infrastructure.Configuration;
using WolfTodo.Tui.Infrastructure.Files;
using WolfTodo.Tui.Infrastructure.Notifications;
using WolfTodo.Tui.Infrastructure.Process;
using WolfTodo.Tui.Infrastructure.Terminal;
using WolfTodo.Tui.Rendering;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<MarkdownTodoProjectReader>();
builder.Services.AddSingleton<ITodoProjectRepository, MarkdownTodoProjectRepository>();
builder.Services.AddSingleton<ProjectCatalogLoader>();
builder.Services.AddSingleton<ProjectBrowserPresenter>();
builder.Services.AddSingleton<BrowserReducer>();
builder.Services.AddSingleton<DayPlannerPresenter>();
builder.Services.AddSingleton<DayPlannerReducer>();
builder.Services.AddSingleton<DayScheduleMarkdownRenderer>();
builder.Services.AddSingleton<IDayScheduleMarkdownFileStore, PhysicalDayScheduleMarkdownFileStore>();
builder.Services.AddSingleton<DayScheduleExportService>();
builder.Services.AddSingleton<IWeeklyTimeLogFileStore, PhysicalWeeklyTimeLogFileStore>();
builder.Services.AddSingleton<WeeklyTimeLogService>();
builder.Services.AddSingleton<IPomodoroCompletionNotifier, PlatformPomodoroCompletionNotifier>();
builder.Services.AddSingleton<IGoogleCalendarEventSourceFactory>(
    new GoogleCalendarEventSourceFactory(GlobalGoogleCalendarTokenPath.Resolve()));
builder.Services.AddSingleton<GoogleCalendarEventMapper>();
builder.Services.AddSingleton<IPlannerCalendarAgendaProvider, GoogleCalendarAgendaProvider>();
builder.Services.AddSingleton<PlannerCalendarAgendaCache>();
builder.Services.AddSingleton<ProjectTodoMutationService>();
builder.Services.AddSingleton<TabHostPresenter>();
builder.Services.AddSingleton<TabHostReducer>();
builder.Services.AddSingleton<ApplicationInputRouter>();
builder.Services.AddSingleton<ApplicationCommandReducer>();
builder.Services.AddSingleton<IExternalEditorLauncher>(new ProcessExternalEditorLauncher());
builder.Services.AddSingleton<IApplicationStateStore>(
    new JsonApplicationStateStore(GlobalApplicationStatePath.Resolve()));
builder.Services.AddSingleton<IProjectFileSystem, PhysicalProjectFileSystem>();
builder.Services.AddSingleton<SurfaceThemeRenderer>();
builder.Services.AddSingleton<StatusRenderer>();
builder.Services.AddSingleton<TodoRowRenderer>();
builder.Services.AddSingleton<CalendarItemRenderer>();
builder.Services.AddSingleton<TerminalInputReader>();
builder.Services.AddSingleton<BrowserRenderer>();
builder.Services.AddSingleton<PlannerRenderer>();
builder.Services.AddSingleton<ITerminalUi>(serviceProvider =>
    new SpectreTerminalUi(
        SafeWindowWidth,
        SafeWindowHeight,
        serviceProvider.GetRequiredService<BrowserRenderer>(),
        serviceProvider.GetRequiredService<PlannerRenderer>(),
        serviceProvider.GetRequiredService<TerminalInputReader>(),
        serviceProvider.GetRequiredService<SurfaceThemeRenderer>()));
builder.Services.AddSingleton<IApplicationConfigurationLoader>(serviceProvider =>
    new TomlApplicationConfigurationLoader(
        GlobalConfigurationPath.Resolve(),
        File.Exists,
        File.ReadAllText));
builder.Services.AddSingleton(serviceProvider =>
    new TuiApplication(
        serviceProvider.GetRequiredService<IApplicationConfigurationLoader>(),
        serviceProvider.GetRequiredService<ProjectCatalogLoader>(),
        serviceProvider.GetRequiredService<ITerminalUi>(),
        serviceProvider.GetRequiredService<IApplicationStateStore>(),
        serviceProvider.GetRequiredService<ApplicationInputRouter>(),
        serviceProvider.GetRequiredService<TabHostPresenter>(),
        serviceProvider.GetRequiredService<TabHostReducer>(),
        serviceProvider.GetRequiredService<ProjectBrowserPresenter>(),
        serviceProvider.GetRequiredService<BrowserReducer>(),
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Assets", "wolf.txt")),
        serviceProvider.GetRequiredService<DayPlannerPresenter>(),
        serviceProvider.GetRequiredService<DayPlannerReducer>(),
        serviceProvider.GetRequiredService<ProjectTodoMutationService>(),
        serviceProvider.GetRequiredService<ApplicationCommandReducer>(),
        externalEditorLauncher: serviceProvider.GetRequiredService<IExternalEditorLauncher>(),
        plannerCalendarCache: serviceProvider.GetRequiredService<PlannerCalendarAgendaCache>(),
        dayScheduleExportService: serviceProvider.GetRequiredService<DayScheduleExportService>(),
        weeklyTimeLogService: serviceProvider.GetRequiredService<WeeklyTimeLogService>(),
        pomodoroCompletionNotifier: serviceProvider.GetRequiredService<IPomodoroCompletionNotifier>()));

using var host = builder.Build();
return host.Services.GetRequiredService<TuiApplication>().Run();

static int SafeWindowWidth()
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

static int SafeWindowHeight()
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
