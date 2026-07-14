# PLAN 0007: Implement Todo Sorting

## Status

Implemented

## Goal

Add a bottom-panel property sort dialog with natural ordering and configurable
launch input while retaining browser structure and selection.

## Implementation

1. Add session sort state, property and direction models, natural text
   comparison, and canonical todo presentation identity.
2. Order names, start dates, normalized tag sets, and Markdown file groups while
   preserving structural groups, nested blocks, completion grouping, and stable
   source ties.
3. Add a configurable `sort_mode` launcher and fixed immediate-apply modal
   choices with source reset and cancellation.
4. Render responsive dialog rows and active-sort hints, reserving their exact
   footer height so the application remains inside the terminal viewport.
5. Verify ordering, selection restoration, configuration, modal routing, and
   terminal layouts with focused tests.
