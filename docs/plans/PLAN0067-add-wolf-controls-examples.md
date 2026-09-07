# PLAN0067: Add Wolf.Controls Examples

## Status

Implemented

## Summary

Add a standalone interactive terminal gallery for Wolf.Controls. The gallery
uses the neutral library theme and a host-owned redraw loop to demonstrate
every current control without referencing Wtodo.

## Delivered

- Numbered, keyboard-navigable gallery for textbox, select list, spinner,
  progress bar, toast, and SplashBox controls.
- Explicit browse and focused-control modes so gallery navigation does not
  conflict with control editing keys.
- Automatic spinner and repeating progress animation; `P` restarts progress
  and `T` triggers a three-second toast.
- Unit tests for gallery reduction, input mapping, and animation scheduling.
- `task controls:examples` launch task and README guidance.
