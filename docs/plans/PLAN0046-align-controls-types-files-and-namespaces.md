# PLAN 0046: Align Controls Types, Files, and Namespaces

## Status

Implemented

## Summary

Apply ADR0012 to the Controls namespace by giving every named type a matching
source file and enforcing its project-relative namespace.

## Changes

- Extend ADR0012 to cover records, enums, interfaces, matching namespaces, and
  public control types.
- Split grouped textbox, multiline textbox, select-list, dialog, and component
  types into matching source files.
- Preserve behavior while completing the MultilineTextBox filename rename.
- Add a source-layout test for the Controls namespace.

## Verification

- Build the solution and run all tests.
