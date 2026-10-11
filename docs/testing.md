# Testing Strategy

## Core

Test package discovery, version identity, assembly/XML parsing, symbol indexing, search ranking/filtering, platform metadata, provenance, native mapping, and error handling without loading native pjproject binaries.

## MCP contract

Test that every tool has a stable schema, validates required inputs, returns deterministic structured output, and exposes useful error information. Include empty-result and ambiguous-result cases.

## stdio host

Run the actual executable and verify MCP initialization, tool/resource discovery, request/response flow, cancellation where applicable, and offline operation against a known local knowledge set.

## HTTP host

Run the ASP.NET host and verify MCP HTTP transport behavior, health/readiness behavior if provided, configuration, and container startup. Avoid coupling tests to a particular public deployment URL.

## Package fixtures

Keep small representative package/API fixtures in tests when feasible. Include at least one generated SWIG-shaped type, a callback/delegate, an enum, a disposable/native-handle style type, and platform-specific metadata. Fixtures must not redistribute third-party material beyond what the repository license permits.

## Validation gates

A roadmap phase is complete only when the relevant tests pass, the solution builds, and documentation accurately describes the resulting behavior.