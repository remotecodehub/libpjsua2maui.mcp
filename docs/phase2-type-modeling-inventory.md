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

## Deterministic mapping coverage

The extractor exposes `PackageExtractionResult.SourceCoverage`; its denominators are calculated from the exact source dictionary inspected, not from examples or estimates:

| Counter | Denominator/meaning |
| --- | --- |
| `ManagedTypesDiscovered` | generated C# type declarations matched |
| `ManagedMembersDiscovered` | generated C# members matched by the source inspector |
| `ManagedMembersApplicableToSwig` | members considered applicable to direct generated-binding inspection |
| `ManagedMembersWithSwigCalls` | distinct managed source members containing a `pjsua2PINVOKE` call |
| `ManagedMembersWithoutDirectSwigCall` | applicable members without a direct call; this is not proof of managed-only behavior |
| `SwigCallsDiscovered` / `SwigCallsResolvedToPInvoke` | direct generated calls and exact declaration resolutions |
| `SwigCallsWithoutPInvoke` | direct calls with no exact platform-local declaration |
| `PInvokeDeclarationsDiscovered` / `PInvokeDeclarationsResolvedToWrapper` | exact `DllImport` declarations and matching C++ entry points |
| `WrapperDefinitionsDiscovered` | generated C++ wrapper definitions |
| `WrapperInvocationsDiscovered` / `WrapperInvocationsResolvedToPjprojectDeclaration` | supported instance/qualified pjproject calls and declaration resolutions |
| `PjprojectDeclarationsResolvedToImplementation` | declaration calls with a matching pjproject implementation |

For the local pinned-source focused validation, each of the four platform source sets produced the same deterministic
managed-to-SWIG counters: 29 types, 50 members/applicable members, 31 members with direct calls, 19 applicable members
without a direct call, 42 direct calls, 3,314 P/Invoke declarations, 3,308 declarations resolved to generated wrapper
definitions, 3,308 wrapper definitions, 57 wrapper invocations, 41 invocations resolved to pjproject declarations,
and 35 declaration/implementation resolutions. These are source-inspection denominators for one `Account`-focused
platform source set, not a claim that the complete package universe has been exhaustively mapped. The four-platform
aggregate for this focused set is 116 types, 200 applicable members, 168 members with calls, 76 without direct calls,
168 calls, 13,256 P/Invoke declarations, 13,232 wrapper resolutions, 13,232 wrapper definitions, 228 wrapper
invocations, 164 declaration resolutions, and 140 implementation resolutions.

The repository inspector supports both generated instance calls such as `(arg1)->create()` and qualified calls such as `pj::Account::notify()`. It binds instance calls through the declared `pj::Type*`/`pj::Type&` receiver rather than using the last type token in the function body. `SourceInspectionDiagnostic` records constructions that are present but unparsed; these are implementation limitations and are not represented as ordinary evidence `Unknown` relationships.

Lifecycle facts are emitted with a managed type node (`managed:<source-path>::<type>`), not merely a source-file node. The facts remain static observations: runtime callback retention, thread affinity, late callbacks and use-after-disposal behavior are not asserted without source evidence.

For the final hardening validation, the direct executable run produced 172 passing tests and the following ReportGenerator summary: 97.4% lines, 90.2% branches (`921/1020`) and 98.1% methods (`158/161`). The HTML report is under `tests/libpjsua2maui.mcp.tests/TestResults/HardeningFinal10/Report/`. These are test-code coverage metrics, while the mapping counters above are source-inspection denominators and must not be conflated.

Relationship status remains evidence-based: `Established` is used for exact resolved links, `Partial` for a demonstrated prefix with a missing native link, `Unknown` for a missing endpoint without sufficient evidence, and `Unparsed` diagnostics for constructs the inspector cannot interpret. No unsupported parser construct is promoted to `Established`.
