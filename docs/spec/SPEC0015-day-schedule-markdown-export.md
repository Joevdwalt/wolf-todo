# SPEC 0015: Day Schedule Markdown Export

## Status

Accepted

## Purpose

Export a selected Day Planner date to an Obsidian-compatible weekly Markdown note without changing Wolf Todo's Markdown todo storage.

## Behavior

Enable export with an optional `[planner.export]` table in global configuration. `notes_directory` is an absolute directory and `project_links` is an ordered list of raw Markdown links. Export writes to `{notes_directory}/YYYY/MM/Week - {ISO-week}.md` and creates missing directories.

The `x` planner binding, configurable through `keybindings.planner_export_schedule`, and the command palette export the selected date. The generated section has an English `# 📅 Weekday, DD Mon YYYY` heading, the configured links, an `## All day` list, and `## Time blocks` containing half-hour rows from 09:00 to 17:00. Timed todos and calendar entries use plain titles; concurrent entries in one block are joined with ` · `.

Date-only todos and calendar all-day entries appear in the All day list. Completed scheduled todos remain included because the export records the day's schedule.

When the weekly note already contains the exact date heading, replace only that section through the next date heading or end of file. Otherwise append the new section, preserving unrelated note content. A missing export configuration disables the palette action and direct export reports a clear error.

## Acceptance Scenarios

1. A selected Monday exports to `YYYY/MM/Week - NN.md` with the configured project links and 16 half-hour time blocks.
2. Todo durations and calendar meetings appear in every overlapping time block; simultaneous entries remain visible on one line.
3. All-day planner items export separately from timed blocks.
4. Re-exporting a date replaces that date's prior section without modifying other weekly-note content.
5. Invalid export configuration prevents startup with a clear diagnostic; a missing export table does not prevent normal planner use.

## References

- [SPEC0009: Day Planner](SPEC0009-day-planner.md)
- [ADR0003: Structure Source Code for Testability](../adr/ADR0003-structure-source-code-for-testability.md)
