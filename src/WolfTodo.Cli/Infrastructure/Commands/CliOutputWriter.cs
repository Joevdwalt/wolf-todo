using System.Text.Json;
using System.Text.Json.Serialization;

namespace WolfTodo.Cli.Infrastructure.Commands;

public sealed class CliOutputWriter(TextWriter output)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public void Write(object value) => output.WriteLine(JsonSerializer.Serialize(value, Options));

    public int Error(int exitCode, string code, string message)
    {
        Write(new { ok = false, error = new { code, message } });
        return exitCode;
    }
}
