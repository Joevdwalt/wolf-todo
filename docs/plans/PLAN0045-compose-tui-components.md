# PLAN 0045: Compose TUI Components

## Status

Implemented

## Summary

Make reusable terminal controls composable through a common state, transition,
measurement, and rendering contract.

## Changes

- Add a public generic component contract with terminal constraints and typed
  semantic transitions.
- Migrate single-line text boxes, multiline text editors, and selectable lists
  to the contract.
- Compose task-editor dialog field controls through the textbox component and
  use component measurement in terminal layouts.
- Keep feature reducers responsible for domain validation and persistence.
- Add standalone multiline-editor and selectable-list harness scenarios.

## Verification

- Cover primitive input transitions, rendering, and sizing with unit tests.
- Verify dialog and terminal layouts continue to compose controls correctly.
- Run the repository test task.
