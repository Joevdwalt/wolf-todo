using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;

namespace WolfTodo.Tui.Infrastructure.Calendar;

public sealed class JsonPlannerCalendarCacheStore(string path) : IPlannerCalendarCacheStore
{
    public IReadOnlyDictionary<DateOnly, PlannerCalendarAgenda> Load(GoogleCalendarConfiguration configuration)
    {
        try
        {
            var saved = JsonSerializer.Deserialize<Snapshot>(File.ReadAllText(path));
            if (saved is { Version: 1 } && saved.Key == Key(configuration) && saved.Agendas is not null)
            {
                return saved.Agendas.Where(entry => entry.Value is not null &&
                        !entry.Value.AllDayItems.IsDefault && !entry.Value.Meetings.IsDefault)
                    .ToDictionary(entry => entry.Key, entry => entry.Value);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            // A missing or damaged cache must not prevent live synchronization.
        }

        return new Dictionary<DateOnly, PlannerCalendarAgenda>();
    }

    public void Save(GoogleCalendarConfiguration configuration, IReadOnlyDictionary<DateOnly, PlannerCalendarAgenda> agendas)
    {
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(new Snapshot(1, Key(configuration), agendas)));
            File.Move(temporaryPath, path, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Disk caching is best effort; keep the live agenda available.
        }
        finally
        {
            try { File.Delete(temporaryPath); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
        }
    }

    private static string Key(GoogleCalendarConfiguration configuration) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
        {
            configuration.OAuthClientFile,
            Calendars = configuration.AdditionalCalendarIds.Order(StringComparer.Ordinal).ToArray()
        }))));

    private sealed record Snapshot(int Version, string Key, IReadOnlyDictionary<DateOnly, PlannerCalendarAgenda> Agendas);
}
