using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WolfTodo.Tui.Features.Splash;
using WolfTodo.Tui.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);
var applicationDirectory = AppContext.BaseDirectory;

builder.Services.AddSingleton<HomeScreenReducer>();
builder.Services.AddSingleton<ITerminalUi, SpectreTerminalUi>();
builder.Services.AddSingleton<IKeybindingsLoader>(serviceProvider =>
    new TomlKeybindingsLoader(
        Path.Combine(applicationDirectory, "keybindings.toml"),
        File.ReadAllText));
builder.Services.AddSingleton(serviceProvider =>
    new TuiApplication(
        serviceProvider.GetRequiredService<IKeybindingsLoader>(),
        serviceProvider.GetRequiredService<ITerminalUi>(),
        serviceProvider.GetRequiredService<HomeScreenReducer>(),
        File.ReadAllText(Path.Combine(applicationDirectory, "Assets", "wolf.txt"))));

using var host = builder.Build();
return host.Services.GetRequiredService<TuiApplication>().Run();
