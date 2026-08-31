using System.Collections.Immutable;
using Tomlyn;
using Tomlyn.Model;

namespace WolfTodo.Cli.Infrastructure;

public sealed class TomlProjectConfigurationLoader(
    string path,
    Func<string, bool> fileExists,
    Func<string, string> readAllText)
{
    public ImmutableArray<string> Load()
    {
        if (!fileExists(path))
        {
            throw new InvalidDataException($"Missing required configuration file: {path}");
        }

        TomlTable document;
        try
        {
            document = Toml.ToModel(readAllText(path));
        }
        catch (Exception exception)
        {
            throw new InvalidDataException($"Invalid configuration file: {exception.Message}", exception);
        }

        if (!document.TryGetValue("projects", out var projectsValue) ||
            projectsValue is not TomlTable projects ||
            !projects.TryGetValue("files", out var filesValue) ||
            filesValue is not TomlArray files ||
            files.Count == 0)
        {
            throw new InvalidDataException(
                "Invalid configuration file: projects.files must contain at least one absolute Markdown file path.");
        }

        var result = ImmutableArray.CreateBuilder<string>();
        foreach (var fileValue in files)
        {
            if (fileValue is not string file ||
                string.IsNullOrWhiteSpace(file) ||
                !Path.IsPathFullyQualified(file) ||
                !string.Equals(Path.GetExtension(file), ".md", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException(
                    "Invalid configuration file: every projects.files value must be an absolute .md file path.");
            }

            result.Add(file);
        }

        return result.ToImmutable();
    }
}
