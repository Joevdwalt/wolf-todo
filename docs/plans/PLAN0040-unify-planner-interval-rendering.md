# Unify planner interval rendering

## Outcome

Day Planner represents item type, time shape, completion, and selection as
separate fields. Tasks, meetings, and calendar events can therefore use the
same interval structure without assuming that a task has a duration.

## Design

- A `PlannerTimelineItemView` exposes item type, start/end times, time shape,
  interval state, completion state, and selection state.
- Timed tasks without `⏱` are instantaneous; explicit durations render start,
  continuation, and end branches. Fifteen-minute intervals use a compact row.
- Calendar entries are classified as meetings when attendees exist, otherwise
  as calendar events.
- Overlaps render as deterministic stacked branches. The first branch is the
  action and inspector selection target.
- Moving a task changes only its scheduled start, preserving its stored
  duration.
- `planner.default_duration_minutes` pre-fills the explicit duration for new
  Planner-created tasks; it no longer changes existing task occupancy.
