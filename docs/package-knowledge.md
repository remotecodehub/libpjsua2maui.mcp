# Package Knowledge Strategy

## Supported package baseline

The MCP requires `libpjsua2maui` version **2.17.0.5 or later** as its minimum supported package version. Version `2.17.0.5` is the first inspected release that publishes the generated C# sources, C++ wrapper sources and headers in the `.nupkg`, and is therefore the minimum baseline for source inspection and Phase 2 mapping. Versions below this baseline must not be treated as complete source-inspection inputs.

The minimum-version requirement is part of the package ingestion contract and must be enforced when acquiring or indexing a package version.

The Core knowledge model represents this package identity and minimum version explicitly. Its manifest, artifacts, symbol representations, documentation records, provenance, and partial relationships are immutable contracts; acquisition, parsing, indexing, and persistence remain later pipeline stages.

## Primary artifact

The published `libpjsua2maui.nupkg` is the consumer contract. Index it by exact version. The indexer should inspect assembly metadata and XML documentation first, then enrich it with package-contained generated artifacts where available.

## Acquisition

`PackageAcquirer` performs the explicit setup/update operation against the NuGet v3 flat-container source. By default, callers should provide an absolute storage directory; artifacts are stored at `<storage>/libpjsua2maui/<version>/libpjsua2maui.<version>.nupkg`. An existing file is revalidated and reused. Downloads are written to a unique temporary file and promoted only after ZIP/package and per-TFM XML validation succeeds. Normal knowledge queries do not invoke acquisition or require network access.

## Assembly and XML extraction

PackageKnowledgeExtractor consumes a validated PackageAcquisitionResult and processes each managed assembly and XML artifact independently by TFM. Assemblies are read from the package ZIP with PEReader and MetadataReader; they are never loaded for execution, so extraction does not require MAUI runtimes or native pjproject binaries.

The extractor records public namespaces, types, nested types, interfaces, classes, structs, enums, delegates, methods, constructors, properties, events, fields, parameter names/types, return types, generic arity, documentation IDs, signatures, package version, TFM, and source artifact. Managed presence remains compile-time metadata only and does not imply native platform availability.

XML is parsed with XDocument/XmlReader with DTD processing disabled. `<summary>` is optional: direct text inside `<member>` is documentation, and `<param>`, `<typeparam>`, `<returns>`, `<value>`, `<remarks>`, `<exception>`, `<example>`, references, and other nested markup are preserved as the member's original inner XML. An empty named member is retained with empty content so its identity is not lost; members without a valid `name` are skipped with no invented ID. Exact XML documentation IDs are associated only with symbols from the same TFM. Valid documentation without a uniquely matching symbol is retained as unassociated documentation; ambiguous or unsupported signature matches are not guessed. Invalid PE metadata or XML raises a diagnostic identifying the package, version, TFM, and artifact.

## XML documentation validation by TFM

During NuGet package acquisition and indexing, the ingestion process must validate that XML documentation is present for **each TFM represented under the package's `lib/` directory**.

For every `lib/<TFM>/` directory containing the `libpjsua2maui` assembly, the package must contain the corresponding XML documentation file alongside the assembly:

```text
lib/
├── net10.0-android/
│   ├── libpjsua2maui.dll
│   └── libpjsua2maui.xml
├── net10.0-ios/
│   ├── libpjsua2maui.dll
│   └── libpjsua2maui.xml
├── net10.0-maccatalyst/
│   ├── libpjsua2maui.dll
│   └── libpjsua2maui.xml
└── net10.0-windows10.0.19041.0/
    ├── libpjsua2maui.dll
    └── libpjsua2maui.xml
```

The validation must therefore be performed **per TFM/platform**, rather than by checking only for the existence of a single XML file somewhere in the package.

A package is considered invalid for indexing if an expected `lib/<TFM>/libpjsua2maui.xml` documentation file is missing for a TFM represented by the package assembly.

The indexer must preserve the TFM association when ingesting the XML documentation. Documentation from one TFM must not be implicitly treated as documentation for another TFM.

The validation helper should expose enough information to report:
- the discovered TFMs;
- the expected XML documentation path for each TFM;
- which TFMs have valid XML documentation;
- which TFMs are missing documentation;
- the package version being validated.

For the current supported package baseline (`2.17.0.5`), the published package is expected to provide one XML documentation file per supported MAUI TFM under `lib/`, together with the source inventory under `src/`.

## Package assets, TFMs, RIDs, and native artifacts

Acquisition discovers artifacts from the package entries rather than from a fixed platform list. Managed assemblies under `lib/<TFM>/` and `runtimes/<RID>/lib/<TFM>/` retain their TFM/RID; `ref/<TFM>/` assemblies are classified separately as reference assemblies. XML documentation beside the package assembly is associated with its `lib` TFM. Files under `runtimes/<RID>/native/` are classified as native libraries only when their native-library extension and native directory context provide that evidence. SWIG/interface and P/Invoke-named files are classified as SWIG artifacts when present, while unknown entries remain `Other`.

The manifest preserves relative package paths, exact package identity/version, and independently optional TFM and RID values. The presence of a managed assembly does not imply a native library or platform support; architectures and platforms remain unknown unless the package path or metadata states them. Discovery is sorted and duplicate-free for a given ZIP, and does not execute or load any package artifact.

## SWIG/native enrichment

`libpjsua2maui` is a C#/.NET MAUI binding over native pjproject. Generated SWIG C# wrappers and interface files are valuable because they explain ownership, callbacks, native handles, and naming transformations. pjproject headers/source may be used to establish native semantics, but only when the relationship is demonstrable.

The knowledge model should be able to represent:

```text
Managed namespace/type/member
        ↓
Generated SWIG C# symbol
        ↓
Wrapper/native symbol
        ↓
pjproject API
        ↓
Platform native artifact
```

Not every node will exist for every symbol; partial mappings are valid.

The Phase 2 model represents these nodes with `KnowledgeNodeReference`. Identified symbols carry a node kind and identifier; textual references are explicitly marked as such, and an unknown endpoint has no fabricated identifier. `KnowledgeRelationship` carries the relationship type, status, both node references, and existing `FactProvenance`. `LifecycleFact` uses the same identity and provenance contract for ownership, native handles, disposal/destruction, callbacks, dependencies, initialization/shutdown order, threading limits, and use-after-disposal risks. These records are persisted in the existing JSON schema shape with optional new fields, so older schema-1 documents without lifecycle facts remain readable.

For the inspected `libpjsua2maui` `v2.17.0.5` producer commit `7dfdd229f944b5f236b27cc19cdc97c3cf717567`, generated `Account.cs` demonstrates SWIG `4.5.1`, `HandleRef`, `swigCMemOwn`, finalization, `Dispose`, `delete_Account`, director connection, delegates, and callback forwarding. The package also publishes `src/Platforms/<platform>/cpp/pjsua2_wrap.cpp`, `pjsua2_wrap.h`, generated C# files, `pjsua2PINVOKE.cs`, and `SWIGTYPE_*` files. The producer repository links `lib/pjproject` at revision `5a457451fa2712ba18e12b01738e8ff3af2b26fd`. These are separate package and repository provenance facts; the MCP resolves declarations and implementations from that checkout when a `SourceRepositoryDescriptor` is supplied and preserves partial/unknown status for genuinely unresolved links.

## Platform metadata

Capture target frameworks, package assets, native libraries, runtime identifiers, architectures, and any build/package evidence that identifies platform availability. Distinguish compile-time managed availability from runtime native availability.

## Provenance

Every enriched fact should carry enough provenance to reproduce the decision: package version, artifact path, source repository/ref when applicable, and mapping confidence. Prefer explicit `Unknown`/`NotAvailable` states over inferred facts.

## Re-indexing

Indexing should be deterministic and repeatable. A package version must produce the same logical model from the same inputs. Do not mutate an existing version's model silently; create a new version or invalidate/rebuild it explicitly.

## Deterministic indexing and persistence

`KnowledgeIndexer` is the Core boundary between `PackageKnowledgeExtractor` and knowledge consumers. It consumes a validated `PackageExtractionResult`; it does not acquire, reopen, or reparse the package. The result is an immutable `IndexedKnowledgeSet` with an `Indexed` manifest status and retains the manifest, artifacts, symbols, complete documentation, associations, and unassociated documentation.

Artifacts, symbols, documentation, and associations are ordered with ordinal, explicit keys. TFM, RID, documentation ID, signature, and relative artifact path participate in those keys, so overloads and platform-specific observations remain distinct. Equivalent duplicate observations are collapsed; conflicting records with one logical identity fail indexing rather than being selected by encounter order.

The canonical offline format is compact UTF-8 JSON written by `JsonKnowledgeStore`. Its schema version is currently **1**, independent of the NuGet package version and the optional knowledge-set revision. The loader rejects unsupported schemas, invalid enum values, missing references, invalid relative artifact paths, and incomplete manifests before returning a set. JSON contains only normalized package-relative paths; acquisition paths and timestamps are not persisted. Save writes a temporary file in the destination directory and promotes it after the complete write, cleaning up after failures where possible.

Loading an existing artifact is offline and has no dependency on `IPackageAcquirer` or `IPackageKnowledgeExtractor`. A setup/update operation may explicitly acquire and extract a package, then call `KnowledgeIndexer` and `JsonKnowledgeStore.SaveAsync` to generate or replace the bundled artifact. Hosts can ship that generated JSON as application content and call `LoadAsync` during startup or query setup. No fabricated baseline artifact is committed by this repository; generation must use a validated acquisition/extraction pipeline for the exact requested package version.

Documentation content is persisted as the complete XML inner content represented by `DocumentationRecord`. A `<summary>` element is not required. Empty named records and valid records without a unique symbol association remain in the documentation or unassociated collections.

The persistence model carries managed-to-SWIG, native and platform evidence with their status, confidence and provenance. Repository mappings are enabled by constructing `PackageKnowledgeExtractor` with a `SourceRepositoryDescriptor`; package-only extraction remains conservative and preserves unresolved endpoints instead of guessing them.

## Offline mode

The stdio host must support a pre-indexed package knowledge set so normal agent queries do not require Internet access. Acquisition may be a separate setup/update operation. The HTTP image may ship a known version or build its index during image creation; runtime network access should not be required for ordinary queries.
