# Phase 2 type-modeling coverage

This inventory records the historical package `libpjsua2maui` `2.17.0.4` and the current source-inspection baseline `2.17.0.5`. Counts are denominator-labelled; they are not runtime compatibility claims. The current producer release commit is `7dfdd229f944b5f236b27cc19cdc97c3cf717567`, with pjproject gitlink `5a457451fa2712ba18e12b01738e8ff3af2b26fd`.

## Package coverage

- `list_package_files`: 60 package entries returned by NugetMcpServer.
- `list_classes_records_structs`: 268 results, `isPartial: false`.
- `list_interfaces`: 0 interfaces returned.
- The type-definition operation was read for `Account`, `Endpoint`, `Call`, `PersistentObject`, and `SWIGTYPE_p_p_void`.
- The extractor examines all public metadata symbols and all four XML documentation files through PE/XML parsing. The separate enum/delegate denominator from the MCP listing is unavailable, so no invented total is reported.

## Generated-source coverage

The source tree contains 315 generated C# files per platform directory for Android, iOS, MacCatalyst and Windows. Four PINVOKE files and 16 SWIGTYPE files were identified. Detailed member correlation was examined for `Account`, `Endpoint`, `Call`, PINVOKE infrastructure, opaque handles and callback-bearing generated types. The full package list is not claimed as source-inspected member by member; unexamined members remain unknown.

| Platform | `Account.cs` SHA-256 | `Endpoint.cs` SHA-256 |
| --- | --- | --- |
| Android | `5E72A4BE2ACD2102F5C17456D1A1CBEA917CC5047007DE41DDAD9FED68E883A7` | `2AF6EC0259C78161C218F0DFF29FDBB16879272D816DCD5E2893BB176B30D7C6` |
| iOS | same as Android | same as Android |
| MacCatalyst | same as Android | same as Android |
| Windows | `9D92D409590D6C33CC3301ADAA290DFF0741D40C85C4FB0FD8222D22261183C0` | `A5F4C7A93125D50ABB834557F4FA7154EFD5019237BDDBEF090E88804D79F482` |

## Relationship and lifecycle status

- Confirmed managed-to-SWIG observations: direct calls from generated managed members are correlated to platform-local `pjsua2PINVOKE` declarations, exact `DllImport` entry points and generated C++ wrapper definitions. Overload suffixes such as `__SWIG_0`/`__SWIG_1` are preserved as exact symbols.
- Native observations: when `PackageKnowledgeExtractor` is configured with the pinned `SourceRepositoryDescriptor`, wrapper bodies are resolved to pjproject declarations and implementations in `lib/pjproject`; missing links remain `Partial` or `Unknown` with no fabricated target identifier.
- Lifecycle facts confirmed in generated source: `HandleRef`, `swigCMemOwn`, `Dispose`, finalizers, conditional `delete_Account`, director connection, and SWIG pending-exception callbacks. These facts are class-specific.
- Unknowns retained explicitly: mappings for wrapper bodies without a resolvable pjproject declaration, and runtime callback/thread behavior. Static source inspection never promotes runtime execution to `ValidatedAvailable`.

## Platform evidence

The extractor emits assembly-level and managed-symbol-level `PlatformEvidence` observations for the four published TFMs: `net10.0-android36.0`, `net10.0-ios26.0`, `net10.0-maccatalyst26.0` and `net10.0-windows10.0.19041`. Each record includes generated C# source, wrapper C++, header and native asset statuses, concrete package paths, confidence, provenance and `NotValidated` runtime status. Package asset presence is distribution evidence; runtime execution remains outside this phase.
