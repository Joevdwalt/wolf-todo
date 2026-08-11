# PLAN 0049: Show the Next-Meeting Countdown

## Goal

Show the time until the next timed Google Calendar event on today's live Day
Planner marker without treating a scheduled todo as a meeting.

## Implementation

- Carry an optional next-meeting duration on the logical current-time row.
- Select the earliest calendar event whose start is later than the displayed
  current time; skip active events and stop at the end of the loaded day.
- Render `NEXT MEETING IN` with compact minute/hour formatting and safely
  truncate the marker label to the available terminal width.
- Preserve the existing current-time marker when calendar data is unavailable
  or contains no later event.
- Document the behavior in the Day Planner specification and README.

## Verification

- Test meeting selection, active and exact-start exclusions, todo isolation,
  countdown formatting, fallback rendering, and narrow-terminal truncation.
- Run `task test` and `git diff --check`.
