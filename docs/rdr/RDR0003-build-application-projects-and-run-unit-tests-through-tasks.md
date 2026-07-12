# RDR 0003: Build Application Projects and Run Unit Tests Through Tasks

## Status

Accepted

## Context

Wolf Todo will contain shared application code and multiple executable hosts.
Maintainers need predictable repository entrypoints that build all production
projects, run every unit test, and show how well those tests exercise
production code. Unit-test feedback and unit-test coverage must remain fast
and deterministic, so they must not depend on real system boundaries such as
the filesystem, terminal, process, network, database, clock, or external
service.

The repository task and script conventions are defined by RDR0002. This record
defines the build and test contracts those tasks must implement. As a new .NET
10 repository, Wolf Todo can adopt Microsoft Testing Platform (MTP) without
VSTest compatibility constraints.

## Decision

`task build` is the supported command for building all production application
projects. It must compile every executable host and shared production library,
including future hosts, and must not build test projects.

`task test` is the supported command for running all unit tests. Unit-test
projects must use the `<ProjectName>.Tests` naming convention. The task must
run every project with that suffix and must not execute integration-test
projects. It must produce a TRX result file for each test project beneath
`artifacts/test-results/`.

Use MTP for test execution. The root `global.json` must set
`test.runner` to `Microsoft.Testing.Platform` so `dotnet test` uses MTP
identically in local and CI environments. Unit-test projects must reference
`xunit.v3.mtp-v2`, `Microsoft.Testing.Extensions.TrxReport`,
`Microsoft.Testing.Extensions.CodeCoverage`, and FluentAssertions. They must
not reference `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`,
`coverlet.collector`, or `coverlet.MTP`.

An integration test is any test that crosses a real system boundary, including
filesystem, terminal or process, network, database, clock, or external-service
access. Integration tests must reside in separately named
`<ProjectName>.IntegrationTests` projects.

`task integration-test` is the supported, explicit opt-in command for running
all integration-test projects. It is not part of `task test`.

`task coverage` is the supported command for collecting unit-test coverage and
generating a report. It must run the same `<ProjectName>.Tests` projects as
`task test` and must not run or include `<ProjectName>.IntegrationTests`
projects. `task test` must not collect coverage or generate a report.

The coverage task must use MTP's code-coverage extension to collect Cobertura
coverage for production assemblies. Test assemblies are excluded from reported
coverage by default and must remain excluded.

Use ReportGenerator as a repository-local .NET tool, recorded in the tool
manifest and restored by the coverage task. Do not require a globally installed
reporting tool. The coverage task must merge all collected Cobertura files and
write these untracked artifacts:

- Raw coverage files beneath `artifacts/coverage/raw/`.
- A merged Cobertura report at `artifacts/coverage/coverage.cobertura.xml`.
- An HTML report with its entry point at
  `artifacts/coverage/report/index.html`.
- A text summary written to the task output that includes line, branch, and
  method coverage.

The coverage task must fail when a unit test, coverage collection, or report
generation fails. It must report coverage but must not enforce a percentage
threshold until the project has established a meaningful baseline.

Unit tests must verify observable behavior using in-memory values, fakes, or
mocks instead of real system-boundary adapters. Tests for adapters that use a
real system boundary are integration tests, even when they exercise a single
class.

When the projects are scaffolded, implement these task contracts with matching
`build.ps1`, `test.ps1`, `integration-test.ps1`, and `coverage.ps1` scripts;
the local .NET tool manifest; and corresponding tasks in `TaskFile.yml`, in
accordance with RDR0002. Exclude `artifacts/coverage/` from version control.

The scripts must invoke `dotnet test` in MTP mode. The test script must pass
`--report-trx` and a results directory beneath `artifacts/test-results/`. The
coverage script must pass `--report-trx`, `--coverage`,
`--coverage-output-format cobertura`, and results/output directories beneath
`artifacts/coverage/`, then pass the resulting Cobertura files to the
repository-local ReportGenerator tool. Use the same commands in CI; CI may
publish the generated TRX and Cobertura files without changing test execution.

## Consequences

### Positive

- `task build` provides one command for compiling all production applications.
- `task test` gives fast feedback without requiring local services, a usable
  terminal, or access to other system resources.
- `task coverage` provides a reproducible, merged report without slowing the
  default unit-test feedback loop.
- Separate test-project suffixes make the default and opt-in test scopes
  explicit and scalable as hosts are added.
- Integration coverage remains available through a deliberate task rather than
  being omitted altogether.

### Negative

- New integration coverage requires a separate test project and a deliberate
  `task integration-test` invocation.
- Some adapter behavior cannot be checked by the default unit-test task.
- Coverage generation adds MTP-extension, reporting-tool, and output-artifact
  maintenance.
- Automation must discover or otherwise maintain the project sets for the
  production, unit-test, and integration-test naming conventions.

## References

- [RDR0001: Document Naming Convention](RDR0001-document-naming-convention.md)
- [RDR0002: Run Repository Scripts Through Tasks](RDR0002-run-repository-scripts-through-tasks.md)
- [ADR0003: Structure Source Code for Testability](../adr/ADR0003-structure-source-code-for-testability.md)
- [Microsoft Testing Platform test reports (.NET)](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-test-reports)
- [Microsoft Testing Platform code coverage (.NET)](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-code-coverage)
- [Microsoft Testing Platform (xUnit.net v3)](https://xunit.net/docs/getting-started/v3/microsoft-testing-platform)
