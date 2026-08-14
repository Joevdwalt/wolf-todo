using System.Collections.Immutable;
using FluentAssertions;
using Google.Apis.Calendar.v3.Data;
using WolfTodo.Tui.Features.Configuration;
using WolfTodo.Tui.Features.DayPlanner;
using WolfTodo.Tui.Infrastructure;

namespace WolfTodo.Tui.Tests.Infrastructure;

public sealed class GoogleCalendarAgendaProviderTests
{
    private static readonly DateOnly Date = new(2026, 8, 14);

    [Fact]
    public async Task LoadAsync_loads_primary_and_additional_calendars_once_and_merges_them_in_time_order()
    {
        var source = new FakeEventSource(new Dictionary<string, ImmutableArray<Event>>
        {
            ["primary"] = [TimedEvent("primary-event", "Later", 11, 0)],
            ["team@example.com"] = [TimedEvent("team-event", "Earlier", 9, 0)]
        });
        var factory = new FakeEventSourceFactory(source);
        var provider = new GoogleCalendarAgendaProvider(factory, new GoogleCalendarEventMapper());
        var configuration = Configuration("team@example.com");

        var agenda = await provider.LoadAsync(configuration, Date, CancellationToken.None);

        agenda.Meetings.Select(meeting => meeting.Title).Should().Equal("Earlier", "Later");
        agenda.Meetings.Select(meeting => meeting.CalendarId)
            .Should().Equal("team@example.com", "primary");
        agenda.SyncState.Should().Be(PlannerCalendarSyncState.Ready);
        agenda.Error.Should().BeNull();
        agenda.Warning.Should().BeNull();
        source.RequestedCalendars.Should().Equal("primary", "team@example.com");
        source.IsDisposed.Should().BeTrue();
        factory.CreateCount.Should().Be(1);
    }

    [Fact]
    public async Task LoadAsync_keeps_successful_calendars_when_an_additional_calendar_fails()
    {
        var source = new FakeEventSource(new Dictionary<string, ImmutableArray<Event>>
        {
            ["primary"] = [TimedEvent("primary-event", "Primary", 10, 0)]
        }, "missing@example.com");
        var provider = new GoogleCalendarAgendaProvider(
            new FakeEventSourceFactory(source),
            new GoogleCalendarEventMapper());

        var agenda = await provider.LoadAsync(
            Configuration("missing@example.com"),
            Date,
            CancellationToken.None);

        agenda.Meetings.Should().ContainSingle().Which.Title.Should().Be("Primary");
        agenda.SyncState.Should().Be(PlannerCalendarSyncState.Ready);
        agenda.Error.Should().BeNull();
        agenda.Warning.Should().Contain("missing@example.com");
    }

    [Fact]
    public async Task LoadAsync_propagates_primary_and_cancellation_failures()
    {
        var primaryFailure = new FakeEventSource(
            new Dictionary<string, ImmutableArray<Event>>(),
            "primary");
        var failedProvider = new GoogleCalendarAgendaProvider(
            new FakeEventSourceFactory(primaryFailure),
            new GoogleCalendarEventMapper());
        var cancellation = new FakeEventSource(
            new Dictionary<string, ImmutableArray<Event>>(),
            "team@example.com",
            cancel: true);
        var cancelledProvider = new GoogleCalendarAgendaProvider(
            new FakeEventSourceFactory(cancellation),
            new GoogleCalendarEventMapper());

        await failedProvider.Invoking(provider => provider.LoadAsync(
                Configuration(), Date, CancellationToken.None))
            .Should().ThrowAsync<InvalidOperationException>();
        await cancelledProvider.Invoking(provider => provider.LoadAsync(
                Configuration("team@example.com"), Date, CancellationToken.None))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task LoadAsync_returns_disabled_without_creating_an_event_source()
    {
        var factory = new FakeEventSourceFactory(new FakeEventSource(
            new Dictionary<string, ImmutableArray<Event>>()));
        var provider = new GoogleCalendarAgendaProvider(factory, new GoogleCalendarEventMapper());

        var agenda = await provider.LoadAsync(
            GoogleCalendarConfiguration.Disabled,
            Date,
            CancellationToken.None);

        agenda.Should().Be(PlannerCalendarAgenda.Disabled);
        factory.CreateCount.Should().Be(0);
    }

    private static GoogleCalendarConfiguration Configuration(params string[] calendarIds) =>
        new(true, "/calendar/oauth.json")
        {
            AdditionalCalendarIds = [.. calendarIds]
        };

    private static Event TimedEvent(string id, string title, int hour, int minute) => new()
    {
        Id = id,
        Summary = title,
        Start = new EventDateTime
        {
            DateTimeDateTimeOffset = new DateTimeOffset(2026, 8, 14, hour, minute, 0, TimeSpan.Zero)
        },
        End = new EventDateTime
        {
            DateTimeDateTimeOffset = new DateTimeOffset(2026, 8, 14, hour, minute, 0, TimeSpan.Zero)
                .AddMinutes(30)
        }
    };

    private sealed class FakeEventSourceFactory(IGoogleCalendarEventSource source)
        : IGoogleCalendarEventSourceFactory
    {
        public int CreateCount { get; private set; }

        public Task<IGoogleCalendarEventSource> CreateAsync(
            string oauthClientFile,
            CancellationToken cancellationToken)
        {
            CreateCount++;
            return Task.FromResult(source);
        }
    }

    private sealed class FakeEventSource(
        IReadOnlyDictionary<string, ImmutableArray<Event>> events,
        string? failingCalendar = null,
        bool cancel = false) : IGoogleCalendarEventSource
    {
        public List<string> RequestedCalendars { get; } = [];

        public bool IsDisposed { get; private set; }

        public Task<ImmutableArray<Event>> LoadEventsAsync(
            string calendarId,
            DateOnly date,
            CancellationToken cancellationToken)
        {
            RequestedCalendars.Add(calendarId);
            if (calendarId == failingCalendar)
            {
                return cancel
                    ? Task.FromException<ImmutableArray<Event>>(new OperationCanceledException())
                    : Task.FromException<ImmutableArray<Event>>(new InvalidOperationException("Unavailable"));
            }

            return Task.FromResult(events.TryGetValue(calendarId, out var found) ? found : []);
        }

        public ValueTask DisposeAsync()
        {
            IsDisposed = true;
            return ValueTask.CompletedTask;
        }
    }
}
