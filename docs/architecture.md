# Architecture

## Goal

Provide agents with reliable, version-aware knowledge about `libpjsua2maui`: its public C# API, generated SWIG layer, native pjproject relationship, platform availability, lifecycle constraints, and useful development examples.

## Solution boundaries

```text
libpjsua2maui.mcp.slnx
  ├─ libpjsua2maui.mcp          Shared MCP/domain implementation
  ├─ libpjsua2maui.mcp.stdio   Local dotnet-tool / stdio host
  └─ libpjsua2maui.mcp.http    HTTP MCP host / container entrypoint
```

The shared project owns package acquisition, metadata extraction, indexing, symbol models, query services, MCP tool/resource definitions, provenance, and validation rules. Hosts own only startup, configuration, dependency composition, and transport-specific protocol integration.

## Hybrid deployment

The stdio executable is the local/offline distribution. It should be usable by an agent without a network dependency after its package knowledge is installed/bundled. The HTTP application is the centralized distribution and is packaged as a Docker image for GHCR. Both hosts must invoke the same shared implementation.

Do not create separate tool implementations for stdio and HTTP. If transport libraries impose different registration mechanisms, adapt them at the host boundary.

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
