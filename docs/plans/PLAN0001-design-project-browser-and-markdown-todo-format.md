# PLAN 0001: Design Project Browser and Markdown Todo Format

## Status

Implemented

## Summary

Design Wolf Todo's first functional feature as a read-only project browser.
Each configured Markdown file is a project containing task-list todos, and a
virtual `All` view aggregates active todos across projects.

## Changes

- Replace executable-adjacent keybindings with a shared, platform-standard
  global TOML configuration containing project directories and keybindings.
- Define top-level Markdown project discovery, project-title front matter, and
  nonfatal source diagnostics.
- Define an Obsidian Tasks-compatible subset for checkbox status, external
  references, priority, tags, start dates, due dates, notes, and subtasks.
- Specify wide, medium, and narrow project-browser layouts with project, todo,
  detail, command, empty, and error states.
- Keep the first feature read-only and defer editing, search, recurrence,
  dependencies, and persistent todo identifiers.

## Verification

- Confirm configuration rules do not conflict across ADR0002, ADR0004,
  SPEC0001, and SPEC0002.
- Confirm the supplied Markdown example parses into the documented todo fields.
- Confirm every responsive layout preserves access to projects, todos, details,
  and commands.
- Validate Markdown formatting and internal links.
