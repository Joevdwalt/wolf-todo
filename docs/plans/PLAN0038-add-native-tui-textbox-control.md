# PLAN 0038: Add Native TUI Text Box Control

## Status

Implemented

## Goal

Provide a reusable, native C# text-editing control for Wolf Todo's terminal UI,
inspired by Ratatui Textarea without introducing a Rust dependency.

## Delivered Design

- `TextBoxState` keeps normalized text, cursor position, and multiline mode.
- `TextBoxReducer` handles printable input, backspace/delete, arrows,
  Home/End, Up/Down, and multiline Enter.
- `TextBoxControl` renders a themed bottom-sheet panel with a focused cursor
  line, vertical viewport, and normal terminal text wrapping.
- Task-note add and edit actions use the multiline box. Subtask titles use its
  single-line mode. The configured save-form binding accepts text into the
  existing task draft; Escape cancels the text edit.
- Markdown parsing and mutation preserve note continuation lines so one note
  can round-trip as a Markdown bullet followed by indented text.

## Deferred

Selection, clipboard support, undo/redo, search, and mouse input remain future
enhancements.
