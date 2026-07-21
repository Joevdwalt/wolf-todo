# PLAN 0039: Render Calendar Meeting Blocks

## Status

Implemented

## Goal

Give read-only Google Calendar meetings the same duration-block treatment as
timed todos and make their practical details visible in the Planner Inspector.

## Delivered Design

- Calendar meetings retain their event identity, location, attendees, and
  description from Google Calendar.
- The earliest meeting in each occupied slot renders as one rounded duration
  block. Concurrent meetings are represented by a `+N` marker.
- Selecting a meeting highlights its entire primary block.
- The fixed-height Inspector shows practical meeting details and concise overlap
  information. Todo details remain primary when a todo shares a meeting slot.

## Constraints

Calendar data remains read-only and does not reserve or block todo slots.
