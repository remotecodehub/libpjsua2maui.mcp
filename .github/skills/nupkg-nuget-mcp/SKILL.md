# Skill: nupkg-nuget-mcp

## Purpose

Use this skill whenever an agent needs to inspect the published `libpjsua2maui.nupkg` through the `NugetMcpServer` MCP server. The NuGet package and the source submodule under `/lib` are separate evidence sources and must not be conflated.

## Prerequisite: configure the MCP server

The recommended local Codex CLI configuration uses the .NET 10 SDK and `dnx`:

```powershell
codex mcp add nuget -- dnx DimonSmart.NugetMcpServer@1.1.7 --yes
codex mcp list
```

Inside Codex, use `/mcp` to confirm the server is available. This configuration is local to the developer's Codex environment; do not commit credentials or machine-specific MCP configuration to this repository.

Docker is optional, not required. If Docker is preferred:

```powershell
docker run -i --rm ghcr.io/dimonsmart/nugetmcpserver:latest
```

Codex MCP configuration for that alternative:

```powershell
codex mcp add nuget-docker -- docker run -i --rm ghcr.io/dimonsmart/nugetmcpserver:latest
```

The Docker runtime pulls the image automatically when it is not present locally. The `latest` tag is mutable; prefer an explicitly versioned image or digest if the publisher provides one and reproducibility is required.

Official project documentation: https://github.com/DimonSmart/NugetMcpServer

## Required inspection workflow

For the baseline used by this repository, inspect package ID `libpjsua2maui` at the exact version `2.17.0.5` or later. Never silently substitute the latest version or another package version. Source-inspection and Phase 2 mapping features require `2.17.0.5` or later.

1. Use `get_package_info` to inspect package metadata for `libpjsua2maui` version `2.17.0.5`.
2. Use `list_package_files` to enumerate the actual files in that exact package version.
3. Use `get_package_file` for relevant package entries, especially:
   - managed assemblies and their TFM paths;
   - XML documentation next to assemblies;
   - native libraries under runtime/RID paths;
   - build targets and other package support files.
4. Use the available type/member inspection tools (for example, `list_classes_records_structs`, `get_class_or_record_or_struct_definition`, `list_interfaces`, and `get_interface_definition`) to examine the API represented by the published assemblies.
5. Record the package ID, exact version, package-relative file path, TFM/RID when known, and which MCP inspection operation supplied the evidence.

Example request to the MCP server:

> Inspect NuGet package `libpjsua2maui`, exact version `2.17.0.5`. Return package metadata, enumerate the files actually present in the .nupkg, and inspect the managed assemblies, generated C# sources, C++ sources, headers, per-TFM XML documentation, native runtime assets, and build targets. Do not replace the requested version with the latest. Distinguish files found in the package from information inferred from assembly metadata.

## Evidence and provenance rules

- The published `.nupkg` is the authority for what package consumers receive.
- The repository submodule at `/lib`, pinned to the producer release matching the package, is source and source-of-generation evidence. It is not proof that a source file is packaged.
- Consult `docs/package-knowledge.md` and `docs/libpjsua2maui-source.md` for the evidence model and pinned source revision.
- Never claim a C# file, SWIG interface, generated wrapper, or native source file is included in the package unless `list_package_files` confirms its package-relative path.
- The build workflow can explain how generated C# and native assets are produced and packed, but workflow steps are not package entries.
- Keep facts from the package and facts from repository source clearly labeled. For each claim, retain version and artifact/path provenance where possible.
- Do not infer exact managed-to-SWIG/native mappings from similar names alone. If the MCP server cannot establish a relationship, mark it as partial or unknown and consult the pinned source/workflow for additional evidence.
- Do not download, execute, or load package assemblies/native libraries merely to inspect metadata unless a separately authorized validation task requires it.

## Expected output

Summarize:
- exact package identity/version inspected;
- package files actually observed, grouped by TFM/RID and asset type;
- public types/members and XML documentation found;
- missing/unavailable evidence;
- any additional repository/workflow evidence used, explicitly separated from package evidence;
- unresolved mappings or platform availability questions without guessing.
