using System.Collections.Immutable;
using FluentAssertions;
using Google.Apis.Calendar.v3.Data;
using WolfTodo.Tui.Infrastructure;

namespace WolfTodo.Tui.Tests.Infrastructure;

public sealed class GoogleCalendarEventSourceFactoryTests
{
    [Fact]
    public async Task CreateAsync_rejects_a_missing_oauth_file_without_creating_a_source()
    {
        var invoked = false;
        var factory = new GoogleCalendarEventSourceFactory(
            oauthClientFile => false,
            (oauthClientFile, cancellationToken) =>
            {
                invoked = true;
                return Task.FromResult<IGoogleCalendarEventSource>(new FakeEventSource());
            });

        await factory.Invoking(candidate => candidate.CreateAsync("/missing.json", default))
            .Should().ThrowAsync<FileNotFoundException>();
        invoked.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_passes_the_oauth_file_and_cancellation_to_source_creation()
    {
        string? capturedPath = null;
        CancellationToken capturedToken = default;
        var expected = new FakeEventSource();
        var factory = new GoogleCalendarEventSourceFactory(
            oauthClientFile => true,
            (oauthClientFile, cancellationToken) =>
            {
                capturedPath = oauthClientFile;
                capturedToken = cancellationToken;
                return Task.FromResult<IGoogleCalendarEventSource>(expected);
            });
        using var cancellation = new CancellationTokenSource();

        var source = await factory.CreateAsync("/calendar/oauth.json", cancellation.Token);

        source.Should().BeSameAs(expected);
        capturedPath.Should().Be("/calendar/oauth.json");
        capturedToken.Should().Be(cancellation.Token);
    }

    private sealed class FakeEventSource : IGoogleCalendarEventSource
    {
        public Task<ImmutableArray<Event>> LoadEventsAsync(
            string calendarId,
            DateOnly date,
            CancellationToken cancellationToken) => Task.FromResult(ImmutableArray<Event>.Empty);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
