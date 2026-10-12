# Testing Strategy

The repository uses one .NET test project, `tests/libpjsua2maui.mcp.tests`, to validate the three production projects. The test project is included in the solution and references the Core, HTTP, and stdio projects.

## Test organization

Tests are organized into three top-level folders matching the production boundaries:

```text
tests/libpjsua2maui.mcp.tests/
├─ MCP/
│  ├─ TestSupport/
│  │  ├─ Fixtures/
│  │  ├─ Mocks/
│  │  ├─ Fakes/
│  │  └─ Helpers/
│  └─ ... MCP/Core tests
├─ Http/
│  ├─ TestSupport/
│  │  ├─ Fixtures/
│  │  ├─ Mocks/
│  │  ├─ Fakes/
│  │  └─ Helpers/
│  └─ ... HTTP host tests
└─ Stdio/
   ├─ TestSupport/
   │  ├─ Fixtures/
   │  ├─ Mocks/
   │  ├─ Fakes/
   │  └─ Helpers/
   └─ ... stdio host tests
```

`MCP`, `Http`, and `Stdio` are the only top-level test areas. Test-support code belongs to the `TestSupport` folder of the area it serves and must not become an implicit shared test utility. If a helper is genuinely shared by two or more areas, it should be moved to a deliberately documented common test-support location rather than copied.

The current repository establishes the three top-level test areas; their `TestSupport` subdirectories are the designated location for fixtures, mocks, fakes, and helpers as those tests are implemented.

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

## Validation gates

A roadmap phase is complete only when the relevant tests pass, the solution builds, and documentation accurately describes the resulting behavior.