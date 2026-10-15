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

### Mandatory coverage validation

Before invoking repository-local .NET tools, run `dotnet tool restore` from the repository root. Local validation is manual: build the test project, execute its generated `.exe` with `dotnet test`, generate Cobertura summaries with ReportGenerator, and inspect line, branch, and method percentages. The test project's `QualityGate` and `QualityGateExecutable` targets are CI-only enforcement targets.

```powershell
dotnet build ./tests/libpjsua2maui.mcp.tests/libpjsua2maui.mcp.tests.csproj -c Release
dotnet test ./tests/libpjsua2maui.mcp.tests/bin/Release/net10.0/libpjsua2maui.mcp.tests.exe -- --report-trx --report-trx-filename test-results.trx --results-directory ./tests/libpjsua2maui.mcp.tests/TestResults/QualityGate --coverlet --coverlet-output-format cobertura
dotnet tool run reportgenerator -reports:"./tests/libpjsua2maui.mcp.tests/TestResults/QualityGate/coverage.cobertura.*.xml" -targetdir:"./tests/libpjsua2maui.mcp.tests/TestResults/QualityGate/Report" -reporttypes:"MarkdownSummary;TextSummary" -classfilters:"+libpjsua2maui.mcp*;-Program"
```

In CI, invoke both CI-only targets to enforce the same threshold:

```powershell
dotnet build ./tests/libpjsua2maui.mcp.tests/libpjsua2maui.mcp.tests.csproj -c Release -t:QualityGate
dotnet build ./tests/libpjsua2maui.mcp.tests/libpjsua2maui.mcp.tests.csproj -c Release -t:QualityGateExecutable
```

Both targets use the configured threshold (90% by default). The supplemental target must fail if tests fail, coverage reports are missing/unparseable, or any coverage dimension is below threshold. Report exact commands, exit statuses, actual percentages, and report locations. Do not lower the threshold, exclude relevant production code, or treat a direct test run as a passing gate.

The generated test executable must be run outside the filesystem/process sandbox when the runner requires named pipes, IPC, child processes, or other host capabilities restricted by the sandbox. If execution is blocked, report the exact command, complete relevant diagnostic, whether tests ran, and whether coverage XML was produced. A zero-test result is not validation.

## Dependencies

Keep dependencies minimal. The core must remain independent from ASP.NET. Transport-specific packages belong in the corresponding host. Avoid adding a dependency solely to save a few lines of straightforward code.

## Documentation

When behavior, public tools, architecture, package ingestion, or release behavior changes, update the corresponding document under `docs/`. Roadmap items should only be marked complete after their stated acceptance criteria are met and the mandatory quality gate passes.
