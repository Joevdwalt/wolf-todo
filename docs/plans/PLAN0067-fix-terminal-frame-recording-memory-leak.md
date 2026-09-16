# PLAN 0067: Fix Terminal Frame Recording Memory Leak

## Goal

Keep `:dump-screen` limited to the current frame without retaining every TUI
render for the lifetime of the process.

## Delivery

1. Replace Spectre's global recorder with a fresh forwarding recorder per frame.
2. Retain only the latest frame as plain text for screen dumps.
3. Restore the live console after successful and failed renders.
4. Isolate renderer test recordings and cover repeated redraws.
