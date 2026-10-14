# Architecture

## Goal

Provide agents with reliable, version-aware knowledge about `libpjsua2maui`: its public C# API, generated SWIG layer, native pjproject relationship, platform availability, lifecycle constraints, and useful development examples.

## Solution boundaries

```text
libpjsua2maui.mcp.slnx
  ├─ src/libpjsua2maui.mcp          Shared MCP/domain implementation
  ├─ src/libpjsua2maui.mcp.stdio    Local dotnet-tool / stdio host
  ├─ src/libpjsua2maui.mcp.http     HTTP MCP host / container entrypoint
  └─ tests/libpjsua2maui.mcp.tests  Tests for Core + stdio + HTTP
```

The shared project owns package acquisition, metadata extraction, indexing, symbol models, query services, MCP tool/resource definitions, provenance, and validation rules. Hosts own only startup, configuration, dependency composition, and transport-specific protocol integration.

Phase 2 enrichment remains in the shared knowledge pipeline. `KnowledgeRelationship` uses typed node references so managed, generated SWIG/wrapper, native pjproject, native artifact, textual, and unknown nodes are not conflated. `LifecycleFact` records ownership, handles, disposal, callbacks, dependencies, ordering, threading constraints, and use-after-disposal risks as evidence-backed observations. Partial, unknown, and conflicting facts are representable; no mapping is inferred from names alone.

## Hybrid deployment

The stdio executable is the local/offline distribution. It should be usable by an agent without a network dependency after its package knowledge is installed/bundled. The HTTP application is the centralized distribution and is packaged as a Docker image for GHCR. Both hosts must invoke the same shared implementation.

Do not create separate tool implementations for stdio and HTTP. If transport libraries impose different registration mechanisms, adapt them at the host boundary.

## Test project boundary

There is one test project, `tests/libpjsua2maui.mcp.tests`, covering all three production projects. Tests are intentionally organized by production boundary:

```text
tests/libpjsua2maui.mcp.tests/
├─ MCP/
│  ├─ TestSupport/
│  │  ├─ Fixtures/
│  │  ├─ Mocks/
│  │  ├─ Fakes/
│  │  └─ Helpers/
│  ├─ ... MCP/Core tests
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

`MCP`, `Http`, and `Stdio` are the test project's three top-level areas. Fixtures, mocks, fakes, stubs, and helpers should remain scoped to the area whose boundary they support. Shared test infrastructure should be introduced explicitly and documented rather than created implicitly through cross-area dependencies.

## Knowledge pipeline

```text
published libpjsua2maui.nupkg
        │
        ├── assemblies / C# metadata
        ├── XML documentation
        ├── generated SWIG artifacts when packaged
        └── package metadata
                 │
                 ▼
          versioned knowledge model
                 │
        ┌────────┴────────┐
        ▼                 ▼
   query services      MCP resources
        │                 │
        └────────┬────────┘
                 ▼
       stdio host / HTTP host
```

Repository source and pjproject source may enrich the model, but package artifacts remain the primary contract for what consumers can actually install.

## Native boundary

The C# API is generated/wrapped through SWIG and ultimately reaches native pjproject binaries compiled for MAUI platforms. The MCP must therefore model the chain:

`consumer C# -> generated SWIG C# -> SWIG/native wrapper -> pjproject C/native platform binary`.

Native execution is outside the initial scope. The MCP should explain and map the native relationship rather than attempt to load platform-specific native binaries inside the Linux HTTP container.

## Version and platform isolation

Every indexed symbol must be associated with a package version. Platform-specific information must identify target framework/RID/architecture when known. Never merge incompatible versions or infer platform support from a symbol's existence alone.

## Dependency direction

The shared project must not reference ASP.NET hosting types. The HTTP host may reference the shared project. The stdio host may reference the shared project. Shared abstractions must remain usable by both.

The test project may reference all three production projects, but production projects must never reference the test project.
