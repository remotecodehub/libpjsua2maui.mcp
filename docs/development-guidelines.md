# Development Guidelines

## General

Implement the smallest coherent change that satisfies the requirement. Read the existing solution before introducing abstractions. Prefer framework-supported .NET facilities over custom infrastructure.

## C# style

- Use modern .NET 10 language/runtime features supported by the solution.
- Prefer primary constructors for dependency injection and simple immutable dependencies.
- Use nullable reference types consistently.
- Prefer `async`/`await` for I/O and cancellation-aware APIs.
- Pass `CancellationToken` through asynchronous call chains.
- Prefer expression-bodied members only where the expression is clearer; do not use compressed single-line method bodies as a general style.
- Keep ordinary methods block-bodied and readable.
- One top-level named type per `.cs` file.
- A common generic implementation and its intentionally coupled generic companion/specialization may share a file when this is materially clearer.
- Prefer records/readonly models for metadata and query results where mutation is not required.
- Prefer collection expressions, pattern matching, switch expressions, and target-typed construction when they improve clarity.
- Avoid premature abstractions and speculative extension points.

## API and errors

Public APIs must have stable contracts and useful XML documentation. Validate user/tool input at boundaries. Errors returned through MCP must explain the failed lookup and, where possible, suggest the appropriate package/version/platform context. Never hide indexing or parsing failures as an empty successful result.

## Package knowledge

Use the exact package version as a first-class key. Preserve source provenance. If a symbol cannot be mapped confidently to a native pjproject symbol, say so rather than guessing. Prefer generated C# metadata for consumer-facing signatures and SWIG/interface/native source only as enrichment.

## MCP design

Tools should answer focused agent questions rather than expose arbitrary file-system access. Results should be structured, concise, and source-aware. Resources should expose stable reference material that an agent can retrieve without repeatedly performing expensive indexing.

## Tests

Core query and parsing behavior must be unit tested. Include version/platform cases. Host tests should verify MCP registration and transport behavior. Tests must not require unavailable native Android/iOS/macOS binaries merely to validate package metadata behavior.

### Mandatory coverage quality gate

Before running tests that depend on repository-local .NET tools, run `dotnet tool restore` from the repository root. The mandatory quality-gate command, also executed from the repository root, is:

```powershell
dotnet build ./tests/libpjsua2maui.mcp.tests/libpjsua2maui.mcp.tests.csproj -c Release -t:QualityGate
```

The test project's custom `QualityGate` target is the authoritative validation command. It executes tests, collects coverage, creates text/Markdown reports, and enforces a minimum of 90% for lines, branches, and methods. Each coverage dimension below 90% must produce its own warning, and any missed threshold must fail the target. Review the generated report and include the actual percentages and command result in implementation summaries. Do not lower the threshold, exclude relevant production code, or substitute plain `dotnet test` for the gate.

If execution is blocked by an environment/tooling limitation, report the exact command and diagnostic to the user and pause the implementation workflow until the user adjusts the target and signals that work may resume. Do not delete or replace the existing `QualityGate` target. If necessary, an additional target may be added to parse the existing Coverlet/ReportGenerator text report for method coverage, but it must preserve the existing target and its rules.

## Dependencies

Keep dependencies minimal. The core must remain independent from ASP.NET. Transport-specific packages belong in the corresponding host. Avoid adding a dependency solely to save a few lines of straightforward code.

## Documentation

When behavior, public tools, architecture, package ingestion, or release behavior changes, update the corresponding document under `docs/`. Roadmap items should only be marked complete after their stated acceptance criteria are met and the mandatory quality gate passes.
