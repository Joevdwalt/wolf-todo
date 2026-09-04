using Google.Apis.Auth.OAuth2;
using Google.Apis.Calendar.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;

namespace WolfTodo.Tui.Infrastructure.Calendar;

public sealed class GoogleCalendarEventSourceFactory : IGoogleCalendarEventSourceFactory
{
    private static readonly string[] Scopes = [CalendarService.Scope.CalendarEventsReadonly];
    private readonly Func<string, bool> fileExists;
    private readonly Func<string, CancellationToken, Task<IGoogleCalendarEventSource>> createSource;

    public GoogleCalendarEventSourceFactory(string tokenDirectory)
        : this(
            File.Exists,
            (oauthClientFile, cancellationToken) =>
                CreateSourceAsync(tokenDirectory, oauthClientFile, cancellationToken))
    {
    }

    public GoogleCalendarEventSourceFactory(
        Func<string, bool> fileExists,
        Func<string, CancellationToken, Task<IGoogleCalendarEventSource>> createSource)
    {
        this.fileExists = fileExists;
        this.createSource = createSource;
    }

    public Task<IGoogleCalendarEventSource> CreateAsync(
        string oauthClientFile,
        CancellationToken cancellationToken)
    {
        if (!fileExists(oauthClientFile))
        {
            throw new FileNotFoundException("Google OAuth client file was not found.", oauthClientFile);
        }

        return createSource(oauthClientFile, cancellationToken);
    }

    private static async Task<IGoogleCalendarEventSource> CreateSourceAsync(
        string tokenDirectory,
        string oauthClientFile,
        CancellationToken cancellationToken)
    {
        await using var clientFile = File.OpenRead(oauthClientFile);
        var secrets = GoogleClientSecrets.FromStream(clientFile).Secrets;
        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            secrets,
            Scopes,
            "wtodo",
            cancellationToken,
            new FileDataStore(tokenDirectory, true));
        var service = new CalendarService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Wolf Todo"
        });
        return new GoogleCalendarEventSource(service);
    }
}
