using FluentAssertions;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Services;
using WolfTodo.Tui.Infrastructure;
using WolfTodo.Tui.Infrastructure.Calendar;

namespace WolfTodo.Tui.Tests.Infrastructure;

public sealed class GoogleCalendarEventSourceTests
{
    [Fact]
    public void CreateRequest_configures_a_single_event_chronological_page()
    {
        using var service = new CalendarService(new BaseClientService.Initializer());
        var start = new DateTimeOffset(2026, 8, 14, 0, 0, 0, TimeSpan.FromHours(2));
        var end = start.AddDays(1);

        var request = GoogleCalendarEventSource.CreateRequest(
            service, "team@example.com", start, end, "page-2");

        request.CalendarId.Should().Be("team@example.com");
        request.TimeMinDateTimeOffset.Should().Be(start);
        request.TimeMaxDateTimeOffset.Should().Be(end);
        request.SingleEvents.Should().BeTrue();
        request.OrderBy.Should().Be(EventsResource.ListRequest.OrderByEnum.StartTime);
        request.ShowDeleted.Should().BeFalse();
        request.PageToken.Should().Be("page-2");
    }

    [Fact]
    public async Task LoadEventsAsync_requests_the_local_day_and_follows_all_pages()
    {
        var requests = new List<(string CalendarId, DateTimeOffset Start, DateTimeOffset End, string? PageToken)>();
        var source = new GoogleCalendarEventSource((calendarId, start, end, pageToken, cancellationToken) =>
        {
            requests.Add((calendarId, start, end, pageToken));
            return Task.FromResult(pageToken is null
                ? new Events
                {
                    Items = [new Event { Id = "first" }],
                    NextPageToken = "page-2"
                }
                : new Events
                {
                    Items = [new Event { Id = "second" }]
                });
        });

        var events = await source.LoadEventsAsync(
            "team@example.com",
            new DateOnly(2026, 8, 14),
            CancellationToken.None);

        events.Select(calendarEvent => calendarEvent.Id).Should().Equal("first", "second");
        requests.Select(request => request.PageToken).Should().Equal(null, "page-2");
        requests.Should().AllSatisfy(request =>
        {
            request.CalendarId.Should().Be("team@example.com");
            request.Start.LocalDateTime.Should().Be(new DateTime(2026, 8, 14));
            (request.End - request.Start).Should().Be(TimeSpan.FromDays(1));
        });
    }

    [Fact]
    public async Task LoadEventsAsync_propagates_api_failures_and_cancellation()
    {
        var failed = new GoogleCalendarEventSource(
            (calendarId, start, end, pageToken, cancellationToken) =>
                Task.FromException<Events>(new InvalidOperationException("Unavailable")));
        var cancelled = new GoogleCalendarEventSource(
            (calendarId, start, end, pageToken, cancellationToken) =>
                Task.FromException<Events>(new OperationCanceledException()));

        await failed.Invoking(source => source.LoadEventsAsync("primary", new DateOnly(2026, 8, 14), default))
            .Should().ThrowAsync<InvalidOperationException>();
        await cancelled.Invoking(source => source.LoadEventsAsync("primary", new DateOnly(2026, 8, 14), default))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task DisposeAsync_disposes_the_owned_calendar_session()
    {
        var disposed = false;
        var source = new GoogleCalendarEventSource(
            (calendarId, start, end, pageToken, cancellationToken) => Task.FromResult(new Events()),
            () =>
            {
                disposed = true;
                return ValueTask.CompletedTask;
            });

        await source.DisposeAsync();

        disposed.Should().BeTrue();
    }
}
