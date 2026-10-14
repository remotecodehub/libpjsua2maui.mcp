# SWIG artifact inventory

This inventory records the evidence collected for Phase 2 Check 1. It is an
observation of exact sources, not a claim that managed symbols have already
been mapped to native pjproject symbols.

## Sources and versions

- MCP repository: branch `main`, commit `3c23786` at the start of the check.
- Complementary library repository: `origin/main` at commit
  `f59260dc020ce9fddc8e914d1f6d274dd8042a5a` when inspected. The local
  checkout before refresh was at `c08fee5` (`v2.17.0.2`); it was clean.
- Published package: `libpjsua2maui` `2.17.0.4`, acquired from the NuGet v3
  flat container and inspected as
  `libpjsua2maui.2.17.0.4.nupkg`. Its nuspec records library commit
  `0351db28f0eee0228cab28188c1a954ee4a12e8f`.

The package is the primary consumer evidence. Repository observations are not
added to the package manifest as if they were published entries.

## Package observations

The package contains 60 ZIP entries. The relevant SWIG-related result is an
absence: no `.i` interface, file with `swig` or `pinvoke` in its path,
`SWIGTYPE_*` source, generated C# wrapper, P/Invoke source, or native wrapper
source was present as a separate package entry.

The package does contain these adjacent binding/runtime artifacts:

- Managed assemblies and XML documentation for
  `net10.0-android36.0`, `net10.0-ios26.0`,
  `net10.0-maccatalyst26.0`, and `net10.0-windows10.0.19041` under `lib/`.
- Android native libraries under `runtimes/android-{arm,arm64,x86,x86_64}/native/`,
  including `libpjsua2.so` and supporting libraries.
- iOS and MacCatalyst static libraries under their respective RID-specific
  `runtimes/*/native/` directories.
- Windows `pjsua2.dll` under `runtimes/win-arm64/native/` and
  `runtimes/win-x64/native/`.
- `build/libpjsua2maui.targets`, which documents how the published native
  assets and Android Java content are selected by target/RID.

These entries are classified by the existing `PackageAcquirer` as managed,
XML, native, package metadata, or `Other`; none is classified as
`ArtifactKind.SwigArtifact` because the package contains no separately
published SWIG artifact.

## Repository-only observations

The complementary repository contains generated SWIG C# sources for Android,
iOS, MacCatalyst, and Windows under
`src/libpjsua2maui/Platforms/<platform>/pjsua2maui/pjsua2/`, including:

- `pjsua2PINVOKE.cs`;
- `SWIGTYPE_p_p_void.cs`, `SWIGTYPE_p_pj_oshandle_t.cs`,
  `SWIGTYPE_p_pjmedia_aud_dev_index.cs`, and
  `SWIGTYPE_p_unsigned_char.cs`; and
- generated wrapper classes such as `Account.cs`.

The files contain the SWIG-generated header identifying SWIG version `4.5.1`.
The repository also contains `res/pj/pjsua2maui/Makefile`, whose inspected
rules invoke SWIG with `-c++ -w312 -csharp -doxygen`, use pjproject headers,
and generate `pjsua2_wrap.cpp` plus managed C# output into a platform-specific
directory. `docs/SWIG.md` documents overriding generated classes. These files
are repository evidence only; their inclusion in package `2.17.0.4` is not
proven by the package ZIP.

## What remains unknown

The package DLLs may contain generated binding implementation details, but
this check does not assign managed-to-SWIG relationships from metadata alone.
The ZIP inventory does not prove the exact native wrapper symbols, ownership,
callback, disposal, handle, lifecycle, or platform confidence semantics.
The model now preserves these observations without claiming that every managed symbol is mapped. Identified and partial nodes can be supplied by an enrichment source, while unresolved endpoints remain explicit. For the source evidence examined in Phase 2, `Account.cs` demonstrates ownership/disposal/handle/director behavior and the Makefile demonstrates the SWIG generation boundary; the package itself still does not publish those source artifacts.

## Reused mechanisms and change

The inventory reused the existing package acquisition/validation and artifact
discovery model. A small classification defect was corrected: a path such as
`build/pinvoke/generated.cs` is now recognized as a SWIG artifact candidate
because `pinvoke` is evaluated across the normalized relative path, while
unknown ordinary C# files remain unclassified. The behavior is covered by the
existing package-acquirer fixture test. No new persisted fields, downloader,
extractor, indexer, MCP tool, or host behavior was introduced.
