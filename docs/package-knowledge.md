# Package Knowledge Strategy

## Supported package baseline

The MCP requires `libpjsua2maui` version **2.17.0.4 or later** as its minimum supported package version. XML documentation comments are available in the published package starting with version `2.17.0.4`; versions below this baseline must not be treated as supported inputs for package knowledge indexing.

The minimum-version requirement is part of the package ingestion contract and must be enforced when acquiring or indexing a package version.

The Core knowledge model represents this package identity and minimum version explicitly. Its manifest, artifacts, symbol representations, documentation records, provenance, and partial relationships are immutable contracts; acquisition, parsing, indexing, and persistence remain later pipeline stages.

## Primary artifact

The published `libpjsua2maui.nupkg` is the consumer contract. Index it by exact version. The indexer should inspect assembly metadata and XML documentation first, then enrich it with package-contained generated artifacts where available.

## Acquisition

`PackageAcquirer` performs the explicit setup/update operation against the NuGet v3 flat-container source. By default, callers should provide an absolute storage directory; artifacts are stored at `<storage>/libpjsua2maui/<version>/libpjsua2maui.<version>.nupkg`. An existing file is revalidated and reused. Downloads are written to a unique temporary file and promoted only after ZIP/package and per-TFM XML validation succeeds. Normal knowledge queries do not invoke acquisition or require network access.

## Assembly and XML extraction

PackageKnowledgeExtractor consumes a validated PackageAcquisitionResult and processes each managed assembly and XML artifact independently by TFM. Assemblies are read from the package ZIP with PEReader and MetadataReader; they are never loaded for execution, so extraction does not require MAUI runtimes or native pjproject binaries.

The extractor records public namespaces, types, nested types, interfaces, classes, structs, enums, delegates, methods, constructors, properties, events, fields, parameter names/types, return types, generic arity, documentation IDs, signatures, package version, TFM, and source artifact. Managed presence remains compile-time metadata only and does not imply native platform availability.

XML is parsed with XDocument/XmlReader with DTD processing disabled. Each member's inner XML is preserved, including summary, remarks, parameter and type-parameter entries, returns, values, exceptions, examples, and nested markup. Exact XML documentation IDs are associated only with symbols from the same TFM. Valid documentation without a uniquely matching symbol is retained as unassociated documentation; ambiguous or unsupported signature matches are not guessed. Invalid PE metadata or XML raises a diagnostic identifying the package, version, TFM, and artifact.

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

For the current supported package baseline (`2.17.0.4`), the published package is expected to provide one XML documentation file per supported MAUI TFM under `lib/`.

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

## Platform metadata

Capture target frameworks, package assets, native libraries, runtime identifiers, architectures, and any build/package evidence that identifies platform availability. Distinguish compile-time managed availability from runtime native availability.

## Provenance

Every enriched fact should carry enough provenance to reproduce the decision: package version, artifact path, source repository/ref when applicable, and mapping confidence. Prefer explicit `Unknown`/`NotAvailable` states over inferred facts.

## Re-indexing

Indexing should be deterministic and repeatable. A package version must produce the same logical model from the same inputs. Do not mutate an existing version's model silently; create a new version or invalidate/rebuild it explicitly.

## Offline mode

The stdio host must support a pre-indexed package knowledge set so normal agent queries do not require Internet access. Acquisition may be a separate setup/update operation. The HTTP image may ship a known version or build its index during image creation; runtime network access should not be required for ordinary queries.
