# Testing Strategy

The repository uses one .NET test project, `tests/libpjsua2maui.mcp.tests`, to validate the three production projects. The test project is included in the solution and references the Core, HTTP, and stdio projects.

## Test organization

Tests are organized into three top-level folders matching the production boundaries:

```text
tests/libpjsua2maui.mcp.tests/
├─ MCP/
│  └─ TestSupport/{Fixtures,Mocks,Fakes,Helpers}/
├─ Http/
│  └─ TestSupport/{Fixtures,Mocks,Fakes,Helpers}/
└─ Stdio/
   └─ TestSupport/{Fixtures,Mocks,Fakes,Helpers}/
```

`MCP`, `Http`, and `Stdio` are the only top-level test areas. Test-support code belongs to the `TestSupport` folder of the area it serves and must not become an implicit shared test utility. If a helper is genuinely shared by two or more areas, move it to a deliberately documented common test-support location rather than copying it.

## Core

Test package discovery, version identity, assembly/XML parsing, symbol indexing, search ranking/filtering, platform metadata, provenance, native mapping, and error handling without loading native pjproject binaries.

## MCP contract

Test that every tool has a stable schema, validates required inputs, returns deterministic structured output, and exposes useful error information. Include empty-result and ambiguous-result cases.

## stdio host

Run the actual executable as a child process and verify MCP initialization, tool/resource discovery, request/response flow, cancellation where applicable, and offline operation against a known local knowledge set. The stdio area includes a process-level CLI healthcheck test and an MCP client integration test that discovers and invokes the `healthcheck` tool.

## HTTP host

Run the actual ASP.NET host as a child process on a dynamically allocated loopback port. Verify `/api/health`, then use the MCP C# client over Streamable HTTP to initialize, discover the `healthcheck` tool, and invoke it at `/mcp`. Avoid coupling tests to a particular public deployment URL.

## Package fixtures

Keep small representative package/API fixtures in the `MCP/TestSupport/Fixtures` area when feasible. Include at least one generated SWIG-shaped type, a callback/delegate, an enum, a disposable/native-handle style type, and platform-specific metadata. Transport-specific fixtures belong under the corresponding `Http/TestSupport/Fixtures` or `Stdio/TestSupport/Fixtures` area. Fixtures must not redistribute third-party material beyond what the repository license permits.

## Test doubles

Use mocks, fakes, and stubs only when they isolate a meaningful boundary. Prefer small fakes for deterministic domain behavior and mocks for interaction verification. Keep test doubles local to the test area whose boundary they represent.

## Mandatory QualityGate

Run commands from the repository root. First restore repository-local .NET tools, including ReportGenerator:

```powershell
dotnet tool restore
```

The original project-level gate remains available:

```powershell
dotnet build ./tests/libpjsua2maui.mcp.tests/libpjsua2maui.mcp.tests.csproj -c Release -t:QualityGate
```

If that MSBuild target cannot be invoked in the local environment, build and run the generated Microsoft.Testing.Platform executable directly to confirm that its Coverlet arguments create a Cobertura XML report. Resolve the actual executable path from the build output; do not assume the filename or output directory:

```powershell
dotnet test ./tests/libpjsua2maui.mcp.tests/bin/Release/net10.0/libpjsua2maui.mcp.tests.exe
```

Then run the executable-driven gate, which runs the built test executable, generates `Summary.txt` and `Summary.md`, prints all three coverage percentages, and enforces the configured threshold:

```powershell
dotnet tool restore
dotnet build ./tests/libpjsua2maui.mcp.tests/libpjsua2maui.mcp.tests.csproj -c Release -t:QualityGateExecutable
```

The `QualityGateExecutable` target is an alternative validation path; it does not replace or weaken `QualityGate`. Both use `QualityGateThreshold` (90 by default) and require at least that percentage for line, branch, and method coverage. Reports are stored under `tests/libpjsua2maui.mcp.tests/TestResults/QualityGate/Report`. A missing/invalid report or test failure must fail the target. If the direct `dotnet test <executable>` invocation creates coverage output in a different location than expected, inspect the actual files and correct the target/output configuration rather than claiming success.

Do not reduce the threshold or exclude relevant production code to make the gate pass. Report the exact commands, exit codes, actual percentages, and report paths. A plain successful build or test run is not a substitute for coverage validation.

If neither gate can execute because of an environment/tooling restriction, stop and notify the user with the exact command and full relevant error. Wait for the user to adjust the target and explicitly authorize resumption.

## Validation gates

A roadmap phase is complete only when the relevant tests pass, the solution builds, the mandatory quality gate passes, and documentation accurately describes the resulting behavior.
