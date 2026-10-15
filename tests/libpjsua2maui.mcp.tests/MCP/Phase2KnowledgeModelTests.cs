using System.Text;
using libpjsua2maui.mcp.Knowledge;

namespace libpjsua2maui.mcp.tests.MCP;

public sealed class Phase2KnowledgeModelTests
{
    [Fact]
    public void NodeReferences_DistinguishIdentifiedTextAndUnknownNodes()
    {
        var managed = new KnowledgeNodeReference(KnowledgeNodeKind.ManagedSymbol, "libpjsua2.maui.Account");
        var swig = new KnowledgeNodeReference(KnowledgeNodeKind.SwigSymbol, "Account");
        var unknown = new KnowledgeNodeReference(KnowledgeNodeKind.NativeSymbol, KnowledgeNodeResolution.Unknown, null);
        var text = new KnowledgeNodeReference(KnowledgeNodeKind.TextReference, KnowledgeNodeResolution.TextReference, "pjsua2");

        Assert.Equal(KnowledgeNodeResolution.Identified, managed.Resolution);
        Assert.Equal(KnowledgeNodeKind.SwigSymbol, swig.Kind);
        Assert.Null(unknown.Identifier);
        Assert.Equal(KnowledgeNodeResolution.TextReference, text.Resolution);
        Assert.Throws<ArgumentException>(() => new KnowledgeNodeReference(KnowledgeNodeKind.NativeSymbol, KnowledgeNodeResolution.Unknown, "guessed"));
    }

    [Fact]
    public async Task IndexAndPersist_PreservesMappingChainLifecycleAndUnknowns()
    {
        var fixture = CreateExtraction();
        var indexed = await new KnowledgeIndexer().IndexAsync(fixture, TestContext.Current.CancellationToken);
        var path = Path.Combine(Path.GetTempPath(), $"phase2-{Guid.NewGuid():N}.json");
        try
        {
            await new JsonKnowledgeStore().SaveAsync(path, indexed, TestContext.Current.CancellationToken);
            var loaded = await new JsonKnowledgeStore().LoadAsync(path, TestContext.Current.CancellationToken);

            var relation = Assert.Single(loaded.Relationships);
            Assert.Equal(KnowledgeFactStatus.Partial, relation.Status);
            Assert.Equal(KnowledgeNodeKind.ManagedSymbol, relation.SourceNode.Kind);
            Assert.Equal(KnowledgeNodeKind.NativeSymbol, relation.TargetNode.Kind);
            Assert.Equal(KnowledgeNodeResolution.Unknown, relation.TargetNode.Resolution);
            var lifecycle = Assert.Single(loaded.LifecycleFacts);
            Assert.Equal(LifecycleFactKind.Disposal, lifecycle.Kind);
            Assert.Equal(ProvenanceKind.RepositorySource, lifecycle.Provenance!.Kind);
            Assert.Contains("destruction", lifecycle.Provenance.Limitations!, StringComparison.Ordinal);
            var platform = Assert.Single(loaded.PlatformEvidence);
            Assert.Equal(KnowledgePlatform.Android, platform.Platform);
            Assert.Equal(PlatformArtifactStatus.Present, platform.ManagedAssembly);
            Assert.Equal(KnowledgeConfidence.Strong, platform.Confidence);
            Assert.Equal(PlatformArtifactStatus.Unknown, platform.ManagedSource);
            Assert.Equal(PlatformRuntimeStatus.NotValidated, platform.Runtime);
            Assert.Contains("lib/net10.0-android/libpjsua2maui.dll", platform.ArtifactPaths);
            Assert.Contains("runtime", platform.Limitations!, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(Path.GetDirectoryName(path)!, Encoding.UTF8.GetString(await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken)));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task Indexer_RejectsConflictingEquivalentMappingsAndForeignFacts()
    {
        var extraction = CreateExtraction();
        var relation = extraction.Relationships.Single();
        var conflicting = new KnowledgeRelationship(
            relation.KnowledgeSet,
            relation.RelationshipType,
            relation.SourceNode,
            relation.TargetNode,
            KnowledgeFactStatus.Established,
            relation.Provenance);
        var duplicate = new PackageExtractionResult(
            extraction.Acquisition,
            extraction.Symbols,
            extraction.Documentation,
            extraction.Associations,
            extraction.UnassociatedDocumentation,
            [relation, conflicting],
            extraction.LifecycleFacts);

        await Assert.ThrowsAsync<InvalidDataException>(() => new KnowledgeIndexer().IndexAsync(duplicate, TestContext.Current.CancellationToken));

        var otherIdentity = new KnowledgeSetIdentity(new PackageIdentity("libpjsua2maui"), PackageVersion.Parse("2.17.0.5"));
        var foreign = new KnowledgeRelationship(otherIdentity, "managed-to-swig", "Account", "Account");
        var foreignExtraction = new PackageExtractionResult(
            extraction.Acquisition,
            extraction.Symbols,
            extraction.Documentation,
            extraction.Associations,
            extraction.UnassociatedDocumentation,
            [foreign]);

        await Assert.ThrowsAsync<InvalidDataException>(() => new KnowledgeIndexer().IndexAsync(foreignExtraction, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void LifecycleAndRelationshipConstructors_ValidateOwnershipAndOptionalValues()
    {
        var extraction = CreateExtraction();
        var identity = extraction.Acquisition.Manifest.Identity;
        var source = new KnowledgeNodeReference(KnowledgeNodeKind.ManagedSymbol, "Example.Type");
        var relation = new KnowledgeRelationship(identity, "managed-to-swig", source, null, KnowledgeFactStatus.Unknown);
        Assert.Null(relation.Target);
        Assert.Equal(KnowledgeNodeResolution.Unknown, relation.TargetNode.Resolution);

        var unknownFact = new LifecycleFact(identity, source, LifecycleFactKind.ThreadingConstraint, KnowledgeFactStatus.Unknown);
        Assert.Null(unknownFact.Value);
        Assert.Null(unknownFact.Provenance);

        var foreign = new KnowledgeSetIdentity(new PackageIdentity("libpjsua2maui"), PackageVersion.Parse("2.17.0.5"));
        Assert.Throws<ArgumentException>(() => new KnowledgeRelationship(identity, "x", "source", provenance: new FactProvenance(foreign, ProvenanceKind.RepositorySource)));
        Assert.Throws<ArgumentException>(() => new LifecycleFact(identity, source, LifecycleFactKind.Ownership, KnowledgeFactStatus.Partial, provenance: new FactProvenance(foreign, ProvenanceKind.RepositorySource)));
    }

    [Fact]
    public void PlatformEvidence_SeparatesArtifactStatusFromConfidence()
    {
        var extraction = CreateExtraction();
        var identity = extraction.Acquisition.Manifest.Identity;
        var subject = new KnowledgeNodeReference(KnowledgeNodeKind.ManagedSymbol, "assembly:example.dll");
        var evidence = new PlatformEvidence(identity, subject, KnowledgePlatform.Windows, "net10.0-windows", "win-x64", PlatformArtifactStatus.Present, PlatformArtifactStatus.Unknown, KnowledgeConfidence.Strong, limitations: "Runtime not executed.");

        Assert.Equal(PlatformArtifactStatus.Present, evidence.ManagedAssembly);
        Assert.Equal(PlatformArtifactStatus.Unknown, evidence.NativeArtifact);
        Assert.Equal(KnowledgeConfidence.Strong, evidence.Confidence);
        Assert.Throws<ArgumentException>(() => new PlatformEvidence(identity, subject, KnowledgePlatform.Windows, "", null, PlatformArtifactStatus.Present, PlatformArtifactStatus.Unknown, KnowledgeConfidence.Unknown));
        Assert.Throws<ArgumentException>(() => new PlatformEvidence(identity, subject, KnowledgePlatform.Windows, "net10.0-windows", null, PlatformArtifactStatus.Present, PlatformArtifactStatus.Unknown, KnowledgeConfidence.Unknown, new FactProvenance(new KnowledgeSetIdentity(new PackageIdentity("libpjsua2maui"), PackageVersion.Parse("2.17.0.5")), ProvenanceKind.PackageArtifact)));
    }

    [Fact]
    public void PersistenceDtos_RejectInvalidTypedEnrichment()
    {
        Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.NodeDto { Kind = "bad", Resolution = "Identified", Identifier = "x" }.ToModel());
        Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.NodeDto { Kind = "ManagedSymbol", Resolution = "bad", Identifier = "x" }.ToModel());
        Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.LifecycleFactDto { Kind = "bad", Status = "Unknown", Subject = new JsonKnowledgeStore.NodeDto { Kind = "ManagedSymbol", Resolution = "Identified", Identifier = "x" } }.ToModel(CreateExtraction().Acquisition.Manifest.Identity));
        Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.LifecycleFactDto { Kind = "Ownership", Status = "bad", Subject = new JsonKnowledgeStore.NodeDto { Kind = "ManagedSymbol", Resolution = "Identified", Identifier = "x" } }.ToModel(CreateExtraction().Acquisition.Manifest.Identity));
        Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.PlatformEvidenceDto { Platform = "bad", ManagedAssembly = "Present", NativeArtifact = "Unknown", Confidence = "Strong", Subject = new JsonKnowledgeStore.NodeDto { Kind = "ManagedSymbol", Resolution = "Identified", Identifier = "x" } }.ToModel(CreateExtraction().Acquisition.Manifest.Identity));

        var identity = CreateExtraction().Acquisition.Manifest.Identity;
        var subject = new JsonKnowledgeStore.NodeDto { Kind = "ManagedSymbol", Resolution = "Identified", Identifier = "x" };
        var lifecycle = new JsonKnowledgeStore.LifecycleFactDto { Subject = subject, Kind = "Ownership", Status = "Established" }.ToModel(identity);
        var platform = new JsonKnowledgeStore.PlatformEvidenceDto { Subject = subject, Platform = "Android", ManagedAssembly = "Present", NativeArtifact = "Unknown", Confidence = "Strong" }.ToModel(identity);
        Assert.Equal(KnowledgeConfidence.Unknown, lifecycle.Confidence);
        Assert.Equal(PlatformRuntimeStatus.NotValidated, platform.Runtime);
    }

    private static PackageExtractionResult CreateExtraction()
    {
        var package = new PackageIdentity("libpjsua2maui");
        var version = PackageVersion.Parse("2.17.0.5");
        var identity = new KnowledgeSetIdentity(package, version, "phase2");
        var assembly = new PackageArtifact(package, version, "lib/net10.0-android/libpjsua2maui.dll", ArtifactKind.ManagedAssembly, "net10.0-android");
        var manifest = new KnowledgeManifest(identity, artifacts: [assembly], status: KnowledgeManifestStatus.Validated);
        var acquisition = new PackageAcquisitionResult(
            Path.Combine(Path.GetTempPath(), "phase2-source.nupkg"),
            manifest,
            new PackageDocumentationValidation(version, new Dictionary<string, string>(), [], []));
        var managed = new KnowledgeNodeReference(KnowledgeNodeKind.ManagedSymbol, "libpjsua2.maui.Account");
        var native = new KnowledgeNodeReference(KnowledgeNodeKind.NativeSymbol, KnowledgeNodeResolution.Unknown, null);
        var source = new FactProvenance(identity, ProvenanceKind.RepositorySource, sourceReference: "src/libpjsua2maui/Platforms/Android/pjsua2maui/pjsua2/Account.cs", limitations: "Generated source does not identify a single pjproject symbol.");
        var relation = new KnowledgeRelationship(identity, "managed-to-native", managed, native, KnowledgeFactStatus.Partial, source);
        var lifecycle = new LifecycleFact(identity, managed, LifecycleFactKind.Disposal, KnowledgeFactStatus.Established, "Dispose clears HandleRef and conditionally invokes delete_Account.", new FactProvenance(identity, ProvenanceKind.RepositorySource, sourceReference: "src/libpjsua2maui/Platforms/Android/pjsua2maui/pjsua2/Account.cs", limitations: "The generated disposal path demonstrates native destruction only when swigCMemOwn is true."));
        var platform = new PlatformEvidence(identity, managed, KnowledgePlatform.Android, "net10.0-android", "android-arm64", PlatformArtifactStatus.Present, PlatformArtifactStatus.Unknown, KnowledgeConfidence.Strong, new FactProvenance(identity, ProvenanceKind.PackageArtifact, assembly.RelativePath, targetFramework: "net10.0-android", runtimeIdentifier: "android-arm64"), "Runtime loading was not executed.", PlatformArtifactStatus.Unknown, PlatformArtifactStatus.Unknown, PlatformArtifactStatus.Unknown, PlatformRuntimeStatus.NotValidated, [assembly.RelativePath]);
        return new PackageExtractionResult(acquisition, [], [], [], [], [relation], [lifecycle], [platform]);
    }
}
