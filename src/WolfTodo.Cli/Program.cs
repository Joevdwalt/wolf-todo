using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using WolfTodo.Cli;
using WolfTodo.Cli.Features;
using WolfTodo.Cli.Infrastructure;
using WolfTodo.Core.Features.ProjectBrowser;
using WolfTodo.Core.Infrastructure.Markdown;

var builder = new HostApplicationBuilder(new HostApplicationBuilderSettings
{
    DisableDefaults = true
});
builder.Services.AddSingleton<MarkdownTodoProjectReader>();
builder.Services.AddSingleton<IProjectFileSystem, PhysicalProjectFileSystem>();
builder.Services.AddSingleton<ITodoProjectRepository, MarkdownTodoProjectRepository>();
builder.Services.AddSingleton<ProjectCatalogLoader>();
builder.Services.AddSingleton<ProjectTodoMutationService>();
builder.Services.AddSingleton(new TomlProjectConfigurationLoader(
    GlobalConfigurationPath.Resolve(),
    File.Exists,
    File.ReadAllText));
builder.Services.AddSingleton<TaskImportService>();
builder.Services.AddSingleton<TaskListService>();
builder.Services.AddSingleton(serviceProvider => new CliApplication(
    serviceProvider.GetRequiredService<TaskImportService>(),
    serviceProvider.GetRequiredService<TaskListService>(),
    Console.In,
    Console.Out,
    File.ReadAllText));
using var host = builder.Build();
return host.Services.GetRequiredService<CliApplication>().Run(args);
