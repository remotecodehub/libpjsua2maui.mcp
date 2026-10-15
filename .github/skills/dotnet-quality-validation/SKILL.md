# Skill: .NET Quality and Validation

## Purpose

Use for every C# implementation task, especially changes to Core package ingestion, indexing, persistence, or provenance.

## Rules

Follow `AGENTS.md` and `docs/development-guidelines.md`. Prefer primary constructors, nullable reference types, async APIs with cancellation, modern collection expressions/pattern matching, and clear block-bodied methods. Avoid compressed single-line methods except genuinely clear expression-bodied members. Keep one named type per file, with the documented generic companion exception.

## Local validation

The `QualityGate` and `QualityGateExecutable` MSBuild targets in the test project are CI-only enforcement targets. Do not invoke them for local/manual validation. The local procedure is:

```powershell
dotnet tool restore
dotnet build ./tests/libpjsua2maui.mcp.tests/libpjsua2maui.mcp.tests.csproj -c Release
dotnet test ./tests/libpjsua2maui.mcp.tests/bin/Release/net10.0/libpjsua2maui.mcp.tests.exe -- --report-trx --report-trx-filename test-results.trx --results-directory ./tests/libpjsua2maui.mcp.tests/TestResults/QualityGate --coverlet --coverlet-output-format cobertura
dotnet tool run reportgenerator -reports:"./tests/libpjsua2maui.mcp.tests/TestResults/QualityGate/coverage.cobertura.*.xml" -targetdir:"./tests/libpjsua2maui.mcp.tests/TestResults/QualityGate/Report" -reporttypes:"MarkdownSummary;TextSummary" -classfilters:"+libpjsua2maui.mcp*;-Program"
```

Validate independently: build exit code, executable test exit code, Cobertura XML presence/parseability, ReportGenerator exit code, and the line/branch/method percentages in `Summary.txt` and `Summary.md`. Compare each percentage with the configured 90% threshold. A successful build or a zero-test result is not a passing validation.

The executable test step must run outside the filesystem/process sandbox when Microsoft Testing Platform or the test runner uses named pipes, IPC, child processes, or other host capabilities restricted by the sandbox. A sandbox `UnauthorizedAccessException` during runner handshake is environment evidence, not a product-test result; rerun the same command with the approved unsandboxed execution context before diagnosing implementation failures.

If the executable is blocked by environment/tooling limitations, report the exact command, full relevant error, failure stage, whether tests ran, and whether coverage XML was produced. Never lower thresholds, exclude relevant production code, or claim a gate pass without evidence.

## CI enforcement

In CI, verify both CI-only targets. They retain the configured 90% line, branch, and method thresholds:

```powershell
dotnet build ./tests/libpjsua2maui.mcp.tests/libpjsua2maui.mcp.tests.csproj -c Release -t:QualityGate
dotnet build ./tests/libpjsua2maui.mcp.tests/libpjsua2maui.mcp.tests.csproj -c Release -t:QualityGateExecutable
```

## Validation workflow

1. Restore repository-local tools with `dotnet tool restore`.
2. Build the test project and execute the manual local procedure.
3. In CI, verify both CI-only targets.
4. If coverage is below threshold, add meaningful tests; do not lower the threshold or exclude relevant production code.
5. Check compiler/analyzer warnings introduced by the change.
6. Update tests, documentation, and roadmap state only when acceptance criteria and the relevant validation have passed.
