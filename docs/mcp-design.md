# MCP Design

## Initial tool surface

The first useful MCP surface should answer questions that materially improve MAUI development accuracy:

- `search_types` — find types by name, namespace, platform, or semantic terms.
- `get_type` — return inheritance, interfaces, constructors, members, documentation, package version, and provenance.
- `get_member` — return an exact method/property/event/field signature and documentation.
- `search_members` — locate members across the package using filters.
- `get_platform_support` — report evidence for Android, iOS, MacCatalyst, and Windows support.
- `get_native_mapping` — explain C# -> SWIG -> native pjproject relationships when known.
- `search_enums` — locate enum types and values with documentation.
- `search_examples` — locate verified usage examples from project/package documentation.
- `get_package_version` — report indexed package and pjproject versions.
- `explain_lifecycle` — return known initialization, ownership, disposal, callback, and threading guidance.

Names may evolve during implementation; preserve the intent and avoid tool proliferation.

## Resources

Resources should provide stable, navigable knowledge such as package manifests, type catalogs, platform matrices, and generated API documentation. Prefer resources for large reference material and tools for targeted queries.

## Result contract

A successful result should contain the answer plus provenance where applicable:

- package version
- target framework/platform
- managed assembly/type/member
- source artifact and path when available
- SWIG interface or generated wrapper reference when available
- pjproject symbol/reference when confidently mapped
- confidence/limitations when mapping is incomplete

## Accuracy rules

The MCP is not an LLM substitute. It should return evidence and structured facts. Do not fabricate signatures, native mappings, lifecycle rules, or platform compatibility. If evidence conflicts across package versions, report the conflict and identify the versions.

## Future capabilities

After the read-only knowledge surface is stable, consider compatibility analysis, API diff between package versions, generated code inspection, and optional isolated validation. Native execution should remain a separate concern because platform binaries cannot safely be treated as portable server dependencies.