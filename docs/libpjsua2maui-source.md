# libpjsua2maui source submodule and NuGet evidence

## Pinned source revision

The repository includes the source repository as a Git submodule at the root path `/lib`:

- Repository: https://github.com/remotecodehub/libpjsua2maui.git
- Release tag: `v2.17.0.4`
- Tag commit (submodule gitlink): `03e4cf1194b810085447d0b1add34fbb5a9c8d58`

The tag `v2.17.0.4` is an annotated tag that resolves to the commit above. Git stores a submodule as a gitlink to a commit, not as a movable tag reference; the pinned commit is therefore the reproducible checkout. The root `.gitmodules` file declares the repository URL.

Initialize the submodule after cloning:

```bash
git submodule update --init --recursive
```

Verify the pin:

```bash
git -C lib rev-parse HEAD
git -C lib describe --tags --exact-match
```

Expected results are commit `03e4cf1194b810085447d0b1add34fbb5a9c8d58` and tag `v2.17.0.4`. Do not advance `/lib` to `main` or another release as part of ordinary work. Updating the source baseline requires an intentional submodule pointer update and a corresponding documentation review.

## Two independent evidence sources

Use both sources when analyzing the package, while preserving their distinct roles:

1. **Published NuGet package** — `libpjsua2maui` version `2.17.0.4`, inspected through the `NugetMcpServer` MCP. This is the consumer contract and the authority for which files and assets are actually distributed.
2. **Pinned source repository** — `/lib` at `v2.17.0.4`. This provides C# generated-source, SWIG generation, native build, and packaging-workflow evidence. A file existing in this checkout does not mean it is included in the NuGet package.

The source repository's `.github/workflows/ci-cd.yml` describes the native dependency/build and SWIG-generation stages, followed by packaging with `dotnet pack`; release-tag runs publish the resulting package. Use the workflow as production/provenance evidence, not as a substitute for inspecting the resulting `.nupkg`.

## Rules for package analysis

- Query the exact package version `2.17.0.4`; never silently switch to the latest version.
- Enumerate package entries through `NugetMcpServer` before making claims about package contents.
- Keep package-relative paths, TFM/RID, package version, and evidence source with each finding where available.
- Clearly label repository-source and workflow facts as such; do not report them as package files.
- Use generated C# and SWIG/native source to enrich a mapping only when the relationship is demonstrable. Otherwise retain a partial or unknown mapping.
- Keep managed compile-time API presence separate from native runtime availability for each MAUI platform.

See `package-knowledge.md` for the knowledge model, package ingestion rules, and provenance requirements. See the `nupkg-nuget-mcp` skill at `.github/skills/nupkg-nuget-mcp/SKILL.md` for the repeatable MCP inspection workflow.
