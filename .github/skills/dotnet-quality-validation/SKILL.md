# Skill: .NET Quality and Validation

## Purpose

Use for every C# implementation task, especially changes to Core package ingestion, indexing, persistence, or provenance.

## Rules

Follow `AGENTS.md` and `docs/development-guidelines.md`. Prefer primary constructors, nullable reference types, async APIs with cancellation, modern collection expressions/pattern matching, and clear block-bodied methods. Avoid compressed single-line methods except genuinely clear expression-bodied members. Keep one named type per file, with the documented generic companion exception.

## Mandatory repository quality gate

Before running the test suite, execute `dotnet tool restore` from the repository root so the repository-local tools (including ReportGenerator) are available.

The canonical validation command must then be run from the repository root:

```powershell
dotnet build ./tests/libpjsua2maui.mcp.tests/libpjsua2maui.mcp.tests.csproj -c Release -t:QualityGate
```

Do not treat a plain `dotnet test`, a successful compilation, or a CI result from an earlier commit as a substitute for this quality gate. The custom `QualityGate` target executes the tests, generates coverage reports, and enforces a minimum of 90% for line, branch, and method coverage. It emits a warning for each coverage dimension below threshold and fails the target when any dimension is below 90%.

Inspect the generated reports under `tests/libpjsua2maui.mcp.tests/TestResults/QualityGate/Report` and report the actual percentages and command exit status. Do not claim the gate passed unless the command exits successfully and all three dimensions meet the threshold.

If the command cannot run because of an environment/tooling restriction, stop before claiming validation or proceeding as though the gate passed. Report the exact command, full relevant error, environment limitation, and the next action needed from the user. The user may adjust the custom target and tell the agent to resume. Do not bypass or remove the existing `QualityGate` target. If a project-level fix is clearly more practical, an additional target may be proposed/implemented to parse the Coverlet/ReportGenerator text report for method coverage while retaining the existing target and its rules.

## Validation workflow

1. Restore repository-local tools from the repository root using `dotnet tool restore`.
2. Run the canonical `QualityGate` command above from the repository root.
3. If the gate fails due to coverage, inspect the report and add meaningful tests for uncovered behavior; do not lower the threshold or exclude relevant production code just to pass.
4. Check compiler/analyzer warnings introduced by the change.
5. Update tests, documentation, and roadmap state only when acceptance criteria and the quality gate have actually passed.
