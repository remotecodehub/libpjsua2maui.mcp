# Phase 2 type-modeling coverage

This inventory records what was actually examined for package `libpjsua2maui` `2.17.0.4` and library revision `03e4cf1194b810085447d0b1add34fbb5a9c8d58` (`v2.17.0.4`). Counts are denominator-labelled; they are not runtime compatibility claims.

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

- Confirmed managed-to-SWIG observations: generated class headers and direct calls from `Account.cs`/`Endpoint.cs` to platform-local `pjsua2PINVOKE` methods; overload suffixes such as `__SWIG_0`/`__SWIG_1` are preserved as textual generated symbols.
- Partial SWIG/native observations: `DllImport("pjsua2")` and named `CSharp_libpjsua2fmaui_*` entry points are confirmed in PINVOKE; the native C++ wrapper body is unavailable, so the exact pjproject endpoint remains unknown.
- Lifecycle facts confirmed in generated source: `HandleRef`, `swigCMemOwn`, `Dispose`, finalizers, conditional `delete_Account`, director connection, and SWIG pending-exception callbacks. These facts are class-specific.
- Unknowns retained explicitly: exact `pjsua2.i` declarations, wrapper C++ implementations, pjproject declaration/implementation chains, and runtime callback/thread behavior.

## Platform evidence

The extractor now emits one `PlatformEvidence` observation per managed TFM. It records managed assembly presence, matching native package asset presence, TFM, platform, confidence, provenance and limitations. Package asset presence is `Confirmed` evidence for distribution; runtime execution remains outside this phase and is explicitly limited in the fact.
