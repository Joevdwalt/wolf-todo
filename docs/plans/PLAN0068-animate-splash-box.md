# PLAN0068: Animate SplashBox

## Status

Implemented

## Summary

Complete the reusable SplashBox control as a centered 800 ms expansion from a
small panel to its available terminal area. It expands horizontally first and
then vertically. The controls gallery and TUI startup host the animation.

## Delivered

- Host-timed, two-phase cubic ease-out expansion at approximately 60 FPS, with
  deterministic size and progress APIs.
- Centered square panel that reveals its title and optional subtitle only after
  expansion completes.
- Full-terminal gallery rendering and `T` replay support.
- Unit coverage for timing, dimensions, gallery selection, and scheduling.
