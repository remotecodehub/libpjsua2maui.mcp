# Roadmap

Status legend: `[ ]` planned, `[~]` in progress, `[x]` complete.

## Phase 0 — Repository foundation

- [x] Define hybrid Core + stdio + HTTP architecture.
- [x] Establish agent documentation and C# rules.
- [x] Establish skills structure.
- [ ] Validate the existing three-project structure against the documented architecture.
- [ ] Verify `libpjsua2maui.mcp` has no ASP.NET host dependency.
- [ ] Verify stdio and HTTP projects reference the shared Core project.
- [ ] Verify host projects contain only composition and transport concerns.
- [ ] Verify target frameworks and package metadata.
- [ ] Preserve the established lowercase project naming convention; do not rename projects solely to satisfy .NET naming conventions.
- [x] Add the dedicated .NET test project covering Core, HTTP, and stdio.
- [x] Establish the `MCP`, `Http`, and `Stdio` test-area organization.
- [x] Define per-area `TestSupport` organization for fixtures, mocks, fakes, stubs, and helpers.
- [x] Establish baseline build/test CI.

## Phase 1 — Package ingestion

- [ ] Define versioned package knowledge model.
- [ ] Implement acquisition of `libpjsua2maui.nupkg`.
- [ ] Extract assembly metadata and XML documentation.
- [ ] Detect package assets, TFMs/RIDs and native artifacts.
- [ ] Implement deterministic indexing and persisted/bundled knowledge.
- [ ] Add provenance to indexed facts.

## Phase 2 — SWIG/pjproject mapping

- [ ] Inspect generated SWIG artifacts available from the package/repository.
- [ ] Model managed-to-SWIG relationships.
- [ ] Model native pjproject mappings where evidence exists.
- [ ] Model ownership, callbacks, disposal, handles, and lifecycle semantics.
- [ ] Add platform-specific evidence and confidence.

## Phase 3 — MCP knowledge surface

- [ ] Implement type/member search.
- [ ] Implement exact type/member inspection.
- [ ] Implement platform support queries.
- [ ] Implement native mapping queries.
- [ ] Implement enum and example search.
- [ ] Implement package/version and lifecycle queries.
- [ ] Add MCP resources for stable reference catalogs.

## Phase 4 — Hosts

- [ ] Implement stdio MCP host.
- [ ] Package stdio host as a dotnet tool.
- [ ] Implement HTTP MCP host using the shared Core.
- [ ] Containerize HTTP host.
- [ ] Add health/readiness behavior where useful.

## Phase 5 — Validation and release

- [ ] Add comprehensive Core tests and fixtures.
- [ ] Add stdio protocol tests.
- [ ] Add HTTP transport tests.
- [x] Add Docker build/verification workflow.
- [x] Publish versioned GHCR images.
- [x] Publish versioned dotnet-tool package.
- [ ] Document agent configuration examples.

## Phase 6 — Advanced accuracy

- [ ] API diff between `libpjsua2maui` versions.
- [ ] Compatibility queries for target MAUI platforms.
- [ ] Verified code/example retrieval.
- [ ] Optional isolated validation tools, kept separate from the read-only knowledge core.

A phase must not be marked complete merely because code exists; its acceptance criteria and validation gates must be satisfied.