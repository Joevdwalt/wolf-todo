# PLAN 0026: Make Schedule the Primary Date

## Status

Implemented

## Goal

Use Planner schedule metadata as the primary date throughout the interactive
application while preserving existing start and due annotations in Markdown.

## Implementation

1. Replace start/due form controls with validated scheduled date and time fields.
2. Carry schedule through shared create/update mutations and reject occupied
   slots against the latest catalog without discarding failed forms.
3. Replace the due column and schedule subline with an adaptive scheduled
   datetime column, and expose schedule in inspectors.
4. Make `d`/`D` sort scheduled datetimes, extend slash filtering to schedule,
   and retain the numeric persisted-sort position.
5. Pre-fill Planner creation from its selected slot and follow successful
   rescheduling to the new date and time.
6. Update specifications, rendering tests, reducer tests, mutation tests, and
   application workflow tests.
