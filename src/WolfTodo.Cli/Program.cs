using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using WolfTodo.Cli;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSingleton<CliApplication>();
using var host = builder.Build();
return host.Services.GetRequiredService<CliApplication>().Run();
