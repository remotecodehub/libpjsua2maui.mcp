# SWIG artifact inventory

This is the reproducible inventory for Phase 2 Check 1. Package evidence and repository evidence are intentionally separate.

## Verified revisions

- Historical inventory: package `2.17.0.4`, submodule `03e4cf1194b810085447d0b1add34fbb5a9c8d58` (`v2.17.0.4`).
- Current Phase 2 baseline: package `2.17.0.5`, producer release commit `7dfdd229f944b5f236b27cc19cdc97c3cf717567` (`v2.17.0.5`).
- Producer pjproject gitlink: `5a457451fa2712ba18e12b01738e8ff3af2b26fd`.

## Published package

The MCP package listing returned 60 entries. It contains four managed DLL/XML pairs for `net10.0-android36.0`, `net10.0-ios26.0`, `net10.0-maccatalyst26.0`, and `net10.0-windows10.0.19041`. It also contains Android native libraries for four RIDs, iOS and MacCatalyst `libpjsua2.a` assets, Windows `pjsua2.dll` for win-arm64/win-x64, `build/libpjsua2maui.targets`, and five Android Java sources.

The historical `2.17.0.4` package contains no SWIG interface file, generated C# source, `pjsua2PINVOKE.cs`, `SWIGTYPE_*` source, or native wrapper source. The current `2.17.0.5` package does contain generated C# sources under `src/`, plus platform C++ wrapper sources and headers under `src/Platforms/<platform>/cpp/`.

`build/libpjsua2maui.targets` explicitly selects native assets by TFM/RID, uses `__Internal` for iOS/MacCatalyst static linking, and copies Windows DLLs for win-x64/win-arm64. This proves packaging/linking intent, not runtime loading success.

## Repository generated sources

At the `v2.17.0.5` producer revision, generated C# is under `src/libpjsua2maui/Platforms/<platform>/pjsua2maui/pjsua2/`, while the package source paths are normalized under `src/Platforms/<platform>/...`:

- Android, iOS, MacCatalyst and Windows each contain 315 generated C# files.
- Each platform has one `pjsua2PINVOKE.cs` with 6,809 lines and four `SWIGTYPE_*` handle/opaque types.
- `Account.cs` and `Endpoint.cs` are byte-identical across Android, iOS and MacCatalyst; Windows has distinct generated files.

The generated files identify SWIG `4.5.1`, use `HandleRef`, `swigCMemOwn`, finalizers and `Dispose`, and call `pjsua2PINVOKE` entry points. The PINVOKE files use `DllImport("pjsua2", ...)`, SWIG exception/string callbacks, and `CSharp_libpjsua2fmaui_*` entry points.

The repository also contains `res/pj/pjsua2maui/Makefile`, which documents SWIG flags, pjproject include roots, `pjsua2.i`/`symbols.i` inputs, `pjsua2_wrap.cpp` generation, and platform-specific `__Internal` selection. The `2.17.0.5` package supplies the generated wrapper and headers directly; the pjproject implementation remains a separate git submodule and must be inspected at its recorded revision.

## Classification boundary

Package entries remain `PackageArtifact` observations, including published managed/native sources. Generated source files, PINVOKE entry points and SWIG opaque types are now package evidence when their `src/` paths are present; Makefile inputs and pjproject files remain repository/submodule evidence unless separately packaged. Native pjproject mappings remain partial unless a wrapper call and native declaration/implementation are both available.
