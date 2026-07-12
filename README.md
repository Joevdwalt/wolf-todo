# Overview

This app is a todo manager that saves files in markdown files. The todo's are markdown todo files

## Structure

main
|__ src (source code)
|__ TaskFile.yml (repository automation tasks)
|__ docs (documentation)
    |__ rdr (repository design decisions)
    |__ adr (architectural design decisions)
    |__ plans (store AI plans here)
    |__ spec (contains functional specs)

## Prerequisites

Maintainers need the following tools before working on the project:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) for building
  and running the application.
- [PowerShell](https://learn.microsoft.com/powershell/) for repository scripts.
- [Task](https://taskfile.dev/) for running the tasks defined in `TaskFile.yml`.

Verify the tools are available:

```text
dotnet --version
pwsh --version
task --version
```

## Development Workflow

Run repository automation through named tasks declared in `TaskFile.yml`.
This includes build, test, formatting, linting, generation, and maintenance
operations. Do not use repository scripts directly as the normal workflow.


## AI Guidance

AGENTS.md files or related files should reference this file for guidence on how to build the application. 
