using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Tui.Features.ApplicationShell;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.ProjectBrowser;
using WolfTodo.Tui.Features.Splash;
using WolfTodo.Tui.Features.Tabs;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<ProjectMarkdownParser>();
builder.Services.AddSingleton<ProjectCatalogLoader>();
builder.Services.AddSingleton<ProjectBrowserPresenter>();
builder.Services.AddSingleton<BrowserReducer>();
builder.Services.AddSingleton<DayPlannerPresenter>();
builder.Services.AddSingleton<DayPlannerReducer>();
builder.Services.AddSingleton<ProjectTodoMutationService>();
builder.Services.AddSingleton<TabHostPresenter>();
builder.Services.AddSingleton<TabHostReducer>();
builder.Services.AddSingleton<ApplicationInputRouter>();
builder.Services.AddSingleton<IApplicationStateStore>(
    new JsonApplicationStateStore(GlobalApplicationStatePath.Resolve()));
builder.Services.AddSingleton<IProjectFileSystem, PhysicalProjectFileSystem>();
builder.Services.AddSingleton<ITerminalUi>(new SpectreTerminalUi());
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
        serviceProvider.GetRequiredService<ProjectTodoMutationService>()));

using var host = builder.Build();
return host.Services.GetRequiredService<TuiApplication>().Run();
