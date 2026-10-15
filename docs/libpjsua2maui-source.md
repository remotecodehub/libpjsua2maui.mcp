# libpjsua2maui source submodule and NuGet evidence

## Pinned source revision

The repository includes the source repository as a Git submodule at the root path `/lib`:

- Repository: https://github.com/remotecodehub/libpjsua2maui.git
- Release tag: `v2.17.0.5`
- Producer release commit: `7dfdd229f944b5f236b27cc19cdc97c3cf717567`
- pjproject submodule revision: `5a457451fa2712ba18e12b01738e8ff3af2b26fd`

The producer release `v2.17.0.5` is the source-inspection baseline. Git stores `lib/pjproject` as a gitlink to a commit, not as a movable tag reference; the pjproject commit is therefore the reproducible native-source checkout. The root `.gitmodules` file declares the producer repository URL, and the producer repository declares the nested pjproject URL.

Initialize the submodule after cloning:

```bash
git submodule update --init --recursive
```

Verify the pin:

```bash
git -C lib rev-parse HEAD
git -C lib describe --tags --exact-match
```

The source-inspection baseline is `v2.17.0.5`. The checked-out producer tag resolves to `7dfdd229f944b5f236b27cc19cdc97c3cf717567`, and its nested `lib/pjproject` gitlink resolves to `5a457451fa2712ba18e12b01738e8ff3af2b26fd`. Package evidence and producer-repository evidence remain separate provenance sources even though they are now aligned to the same release.

## Two independent evidence sources

Use both sources when analyzing the package, while preserving their distinct roles:

1. **Published NuGet package** — `libpjsua2maui` version `2.17.0.5`, inspected through the `NugetMcpServer` MCP. This is the consumer contract and the authority for which files and assets are actually distributed.
2. **Producer source repository** — `/lib` at `v2.17.0.5`, with `lib/pjproject` at `5a457451fa2712ba18e12b01738e8ff3af2b26fd`. This provides C# generated-source, C++ wrapper, headers, SWIG generation, native build, and pjproject provenance evidence. A file existing in this checkout does not mean it is included in the NuGet package.

The source repository's `.github/workflows/ci-cd.yml` describes the native dependency/build and SWIG-generation stages, followed by packaging with `dotnet pack`; release-tag runs publish the resulting package. Use the workflow as production/provenance evidence, not as a substitute for inspecting the resulting `.nupkg`.

The Core mapping pipeline accepts a `SourceRepositoryDescriptor` for this checked-out producer and nested pjproject revision. When supplied to `PackageKnowledgeExtractor`, it correlates direct generated C# calls with exact `pjsua2PINVOKE` declarations, wrapper definitions, pjproject declarations and implementations. Repository relationships carry the producer repository, tag/revision and pjproject revision. Package-only extraction does not invent repository mappings; it retains unresolved endpoints as `Partial` or `Unknown`.

## Rules for package analysis

- Query the exact package version `2.17.0.5` for Phase 2 source inspection; never silently switch to the latest version.
- Enumerate package entries through `NugetMcpServer` before making claims about package contents.
- Keep package-relative paths, TFM/RID, package version, and evidence source with each finding where available.
- Clearly label repository-source and workflow facts as such; do not report them as package files.
- Use generated C# and SWIG/native source to enrich a mapping only when the relationship is demonstrable. Otherwise retain a partial or unknown mapping.
- Keep managed compile-time API presence separate from native runtime availability for each MAUI platform.

See `package-knowledge.md` for the knowledge model, package ingestion rules, and provenance requirements. See the `nupkg-nuget-mcp` skill at `.github/skills/nupkg-nuget-mcp/SKILL.md` for the repeatable MCP inspection workflow.
