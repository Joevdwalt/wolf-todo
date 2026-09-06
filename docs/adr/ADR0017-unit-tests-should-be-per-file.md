# ADR 0017: Unit tests should be per file

## Status

Accepted

## Context

Unit tests should be per class. This makes it easy to find methods that are being tested.
Example

```text
Wtodo.Controls
  |__TextBox.cs

Wtodo.Controls.Test
  |__TextboxTest.cs

```
The test class would contain all the tests for the TextBox.cs class. The test project should mimic the implementations form including folders and namespaces

## Decision

- Implement new test structure as per the context. 

## Consequences

- No logical grouping, but grouping per class
