# Skill: .NET Quality and Validation

## Purpose

Use for every C# implementation task, especially changes to Core package ingestion, indexing, persistence, or provenance.

## Rules

Follow `AGENTS.md` and `docs/development-guidelines.md`. Prefer primary constructors, nullable reference types, async APIs with cancellation, modern collection expressions/pattern matching, and clear block-bodied methods. Avoid compressed single-line methods except genuinely clear expression-bodied members. Keep one named type per file, with the documented generic companion exception.

## Mandatory repository quality gate

Run `dotnet tool restore` from the repository root before invoking repository-local tools.

First attempt the canonical project-level target:

```powershell
dotnet build ./tests/libpjsua2maui.mcp.tests/libpjsua2maui.mcp.tests.csproj -c Release -t:QualityGate
```

If the environment fails to invoke that target, do not infer that tests or code compilation failed merely from a generic MSBuild failure. Capture the complete diagnostic and use the executable-driven fallback without removing or weakening the original target.

Resolve the actual generated test executable path from the build output, then run it using `dotnet test <path-to-test-executable>` to verify execution and Cobertura XML generation. For example, on Windows:

```powershell
dotnet test ./tests/libpjsua2maui.mcp.tests/bin/Release/net10.0/libpjsua2maui.mcp.tests.exe
```

Do not assume this direct invocation also runs ReportGenerator or enforces the quality threshold. Then run the supplemental target that executes the test binary, creates the summary reports, and validates the configured threshold:

```powershell
dotnet tool restore
dotnet build ./tests/libpjsua2maui.mcp.tests/libpjsua2maui.mcp.tests.csproj -c Release -t:QualityGateExecutable
```

The existing `QualityGate` target must remain intact. Both targets use the configured threshold (90 by default) for line, branch, and method coverage. Inspect `TestResults/QualityGate/Report/Summary.txt` and `Summary.md`; report real percentages, exact command exit statuses, and report paths. Never claim success unless the executable completed successfully, the reports contain valid metrics, and all three dimensions meet the threshold.

If either invocation is blocked by environment/tooling limitations, report the exact command, full relevant error, failure stage, whether tests ran, and whether coverage XML was produced. Pause and wait for the user to adjust the target and authorize resumption. Never lower thresholds, exclude relevant production code, or claim a gate pass without evidence.

## Validation workflow

1. Restore repository-local tools from the repository root using `dotnet tool restore`.
2. Attempt the canonical `QualityGate` target.
3. If it is blocked by environment/target execution, inspect the diagnostic and use the direct executable invocation plus `QualityGateExecutable` fallback.
4. If coverage is below threshold, inspect the report and add meaningful tests for uncovered behavior; do not lower the threshold or exclude relevant production code.
5. Check compiler/analyzer warnings introduced by the change.
6. Update tests, documentation, and roadmap state only when acceptance criteria and the quality gate have actually passed.
