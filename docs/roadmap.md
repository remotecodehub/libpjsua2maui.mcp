# Roadmap

Status legend: `[ ]` planned, `[~]` in progress, `[x]` complete.

## Phase 0 — Repository foundation

- [x] Define hybrid Core + stdio + HTTP architecture.
- [x] Establish agent documentation and C# rules.
- [x] Establish skills structure.
- [x] Validate the existing three-project structure against the documented architecture.
- [x] Verify `libpjsua2maui.mcp` has no direct ASP.NET host dependency.
- [x] Verify stdio and HTTP projects reference the shared Core project.
- [x] Keep host projects focused on startup, dependency composition, health checks, and transport concerns.
- [x] Verify target frameworks and package metadata use .NET 10 and consistent host versions.
- [x] Preserve the established lowercase project naming convention; do not rename projects solely to satisfy .NET naming conventions.
- [x] Add the dedicated .NET test project covering Core, HTTP, and stdio.
- [x] Establish the `MCP`, `Http`, and `Stdio` test-area organization.
- [x] Define per-area `TestSupport` organization for fixtures, mocks, fakes, stubs, and helpers.
- [x] Establish baseline build/test CI.
- [x] Implement DI-based stdio host startup using the official MCP SDK and stdio transport.
- [x] Route stdio host logs to stderr so stdout remains reserved for MCP protocol messages.
- [x] Add a `healthcheck` CLI command that reports status, transport, and version as JSON.
- [x] Expose one shared `healthcheck` MCP tool from the Core project for both transports.
- [x] Implement the HTTP MCP Streamable HTTP endpoint at `/mcp`.
- [x] Preserve the HTTP health endpoint at `/api/health`.
- [x] Add a unit test for the stdio health-check service.
- [x] Add a process-level test for CLI healthcheck output and exit code.
- [x] Add stdio MCP protocol integration coverage for initialization, tool discovery, and invocation.
- [x] Add HTTP host integration coverage for `/api/health`.
- [x] Add HTTP MCP protocol integration coverage for initialization, tool discovery, and invocation.
- [x] Run the updated solution build and complete test suite successfully.
- [x] Confirm the latest GitHub Actions build/test run succeeds after these changes.

## Phase 1 — Package ingestion

- [x] Define versioned package knowledge model.
- [x] Implement acquisition of `libpjsua2maui.nupkg`.
- [x] Extract assembly metadata and XML documentation.
- [x] Detect package assets, TFMs/RIDs and native artifacts.
- [x] Implement deterministic indexing and persisted/bundled knowledge.
- [x] Add provenance to indexed facts.

## Phase 2 — SWIG/pjproject mapping

### Phase 2A — Evidence universe and exhaustive inventory

Phase 2A is complete. The deterministic inventory model, metadata classification, TFM evidence deduplication, XML/source correlation, counters, and focused tests passed the manual validation procedure outside the sandbox. Metadata enumeration is now explicitly proven when all primary managed assemblies are inspected; source/XML correlation remains a separate evidence dimension. Phase 2B is implemented locally and awaits the user's manual CI validation; Phase 2C--2E remain pending.

- [x] Inspect generated SWIG artifacts available from the package/repository. Baseline `2.17.0.5` publishes generated C#, C++ and headers; the producer tag and pjproject gitlink are recorded separately.
- [x] Establish the complete deterministic package symbol universe and immutable denominators for all relevant types and members.
- [x] Inventory classes, records, structs, enums, delegates, interfaces and other relevant metadata symbols with origin/provenance.
- [x] Inventory relevant constructors, properties, fields, methods, events, operators, static members and inherited/publicly exposed members.
- [x] Correlate package metadata, XML documentation, TFM/platform evidence and generated source into one deterministic inventory without double-counting.
- [x] Add an explicit inspection status for every inventory item (`NotInspected`, `Inspected`, `Mapped`, `Partial`, `Unknown`, `NotApplicable`, `Unparsed` or equivalent deterministic states).
- [x] Add deterministic inventory counters and an evidence-closure report that can prove whether every relevant package symbol was considered.

### Phase 2B — Exhaustive managed-to-SWIG mapping

Status: `[~]` local implementation; remote/CI validation is intentionally not claimed.

- [ ] Correlate every applicable managed type and member, not only representative types such as `Account`, `Endpoint` and `Call`.
- [ ] Classify type origin so managed-only/generated/helper/opaque/enum/delegate/native-backed types are not incorrectly forced through the same C# → SWIG → P/Invoke → wrapper chain.
- [ ] Resolve direct generated C# member calls to exact `pjsua2PINVOKE` declarations and P/Invoke entry points where source evidence exists.
- [ ] Preserve overloads, generic/type information and exact generated entry-point names in relationships.
- [ ] Replace bare `Unknown` outcomes with deterministic evidence-gap reasons and provenance.
- [ ] Resolve parser limitations before declaring an evidence gap; retain `Unparsed` only when the source construct is genuinely unsupported and document the exact limitation.

### Phase 2C — Exhaustive native mapping

- [ ] Correlate every applicable P/Invoke declaration to generated wrapper definitions and wrapper invocations.
- [ ] Resolve wrapper invocations to pjproject declarations and implementations when the producer/pjproject sources provide sufficient evidence.
- [ ] Record the exact last proven edge for every `Partial` relationship and the missing next edge.
- [ ] Distinguish missing source/artifact evidence from ambiguous or unsupported correlation instead of collapsing them into `Unknown`.
- [ ] Add native binary linkage evidence where the available package artifacts can prove it; otherwise record an explicit evidence gap rather than inferring linkage.

### Phase 2D — Lifecycle, ownership and behavioral evidence model

- [ ] Model creation, ownership, native handles, `HandleRef`, `swigCMemOwn`, `Dispose`, finalizers and `delete_*` relationships per applicable type.
- [ ] Model callback registration/unregistration, delegate retention, director evidence and callback-related lifetime relationships where statically demonstrable.
- [ ] Identify parent/child ownership and thread-affinity/lifetime semantics only when supported by source/artifact evidence; do not infer runtime behavior from naming alone.
- [ ] Represent runtime-only semantics separately from static evidence and keep runtime validation explicitly `NotValidated` until an actual runtime validation path exists.

### Phase 2E — Evidence gate and completion criteria

- [ ] Define concrete `EvidenceGapReason` categories covering missing generated source, missing managed call, missing P/Invoke declaration, missing wrapper definition/invocation, missing pjproject declaration/implementation, missing native binary evidence, unsupported source construct, ambiguous overload/native target, runtime-only behavior and insufficient artifacts.
- [ ] Require provenance for every `Established`, `Partial`, `EvidenceGap`, `NotApplicable`, `Unparsed` and `NotValidated` result.
- [ ] Add a Phase 2 evidence gate independent from automated test-code coverage: 100% relevant types classified, 100% relevant members classified, 100% applicable relationships given deterministic outcomes, and zero unexplained `Unknown`/`Unparsed` results.
- [ ] Add deterministic validation that all `Partial`/evidence-gap outcomes identify the exact missing evidence and last proven relationship edge.
- [ ] Update Phase 2 documentation with final denominators, coverage counters, unresolved evidence gaps and reproducible validation commands.
- [ ] Mark Phase 2 complete only after the exhaustive inventory, mapping, evidence-gap classification and evidence gate pass; runtime behavior may remain `NotValidated` when explicitly documented.

## Phase 3 — MCP knowledge surface

- [ ] Implement type/member search.
- [ ] Implement exact type/member inspection.
- [ ] Implement platform support queries.
- [ ] Implement native mapping queries.
- [ ] Implement enum and example search.
- [ ] Implement package/version and lifecycle queries.
- [ ] Add MCP resources for stable reference catalogs.

## Phase 4 — Hosts

- [ ] Package stdio host as a dotnet tool.
- [ ] Containerize HTTP host.
- [ ] Add health/readiness behavior where useful.

## Phase 5 — Validation and release

- [ ] Add comprehensive Core tests and fixtures.
- [ ] Document agent configuration examples.
- [x] Add stdio protocol tests.
- [x] Add HTTP transport tests.
- [x] Add Docker build/verification workflow.
- [x] Publish versioned GHCR images.
- [x] Publish versioned dotnet-tool package.

## Phase 6 — Advanced accuracy

- [ ] API diff between `libpjsua2maui` versions.
- [ ] Compatibility queries for target MAUI platforms.
- [ ] Verified code/example retrieval.
- [ ] Optional isolated validation tools, kept separate from the read-only knowledge core.

A phase must not be marked complete merely because code exists; its acceptance criteria and validation gates must be satisfied.
