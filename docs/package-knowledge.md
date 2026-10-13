# Package Knowledge Strategy

## Supported package baseline

The MCP requires `libpjsua2maui` version **2.17.0.4 or later** as its minimum supported package version. XML documentation comments are available in the published package starting with version `2.17.0.4`; versions below this baseline must not be treated as supported inputs for package knowledge indexing.

The minimum-version requirement is part of the package ingestion contract and must be enforced when acquiring or indexing a package version.

## Primary artifact

The published `libpjsua2maui.nupkg` is the consumer contract. Index it by exact version. The indexer should inspect assembly metadata and XML documentation first, then enrich it with package-contained generated artifacts where available.

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
