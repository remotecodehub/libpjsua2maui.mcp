using System.Collections.ObjectModel;
using libpjsua2maui.mcp.Knowledge;

namespace libpjsua2maui.mcp.tests.MCP;

public sealed class KnowledgeModelTests
{
    private static readonly PackageIdentity Package = new("libpjsua2maui");

    [Fact]
    public void PackageVersion_AcceptsMinimumAndLaterVersionsWithNumericPrecedence()
    {
        var minimum = PackageVersion.Parse("2.17.0.4");
        var later = PackageVersion.Parse("2.17.0.10");

        Assert.Equal("2.17.0.4", minimum.Value);
        Assert.True(later.CompareTo(minimum) > 0, $"later={later} minimum={minimum} comparison={later.CompareTo(minimum)}");
        Assert.True(PackageVersion.Parse("2.17.0.11-beta.1").CompareTo(later) > 0);
    }

    [Theory]
    [InlineData("2.17.0.3")]
    [InlineData("2.16.99.99")]
    public void PackageVersion_RejectsVersionsBeforeTheSupportedBaseline(string value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => PackageVersion.Parse(value));
    }

    [Fact]
    public void PackageVersion_RejectsMissingValue()
    {
        Assert.Throws<ArgumentException>(() => PackageVersion.Parse(""));
    }

    [Theory]
    [InlineData("not-a-version")]
    [InlineData("2.17.0.4.1")]
    [InlineData("2.17.0.04")]
    [InlineData("2..17.0.4")]
    [InlineData("2.17.0.4-")]
    [InlineData("2.17.0.4-alpha_")]
    [InlineData("+2.17.0.4")]
    public void PackageVersion_RejectsMalformedValues(string value)
    {
        Assert.Throws<FormatException>(() => PackageVersion.Parse(value));
    }

    [Fact]
    public void PackageVersion_OrdersPrereleaseIdentifiersByNuGetRules()
    {
        Assert.True(PackageVersion.Parse("2.17.0.5-alpha").CompareTo(PackageVersion.Parse("2.17.0.5-alpha.1")) < 0);
        Assert.True(PackageVersion.Parse("2.17.0.5-alpha.1").CompareTo(PackageVersion.Parse("2.17.0.5-alpha.beta")) < 0);
        Assert.True(PackageVersion.Parse("2.17.0.5-alpha.beta").CompareTo(PackageVersion.Parse("2.17.0.5-beta")) < 0);
        Assert.Equal(1, PackageVersion.Parse("2.17.0.4").CompareTo(null));
        Assert.True(PackageVersion.Parse("2.17.0.4").Equals(PackageVersion.Parse("2.17.0.4")));
        Assert.False(PackageVersion.Parse("2.17.0.4").Equals(null));
        Assert.True(PackageVersion.Parse("2.17.0.5-alpha.1").CompareTo(PackageVersion.Parse("2.17.0.5-alpha")) > 0);
    }

    [Theory]
    [InlineData("2.17.0.5-alpha", "2.17.0.5-alpha.1")]
    [InlineData("2.17.0.5-alpha.1", "2.17.0.5-alpha.2")]
    [InlineData("2.17.0.5-alpha.1", "2.17.0.5-alpha.10")]
    [InlineData("2.17.0.5-alpha", "2.17.0.5")]
    [InlineData("2.17.0.5-alpha", "2.17.0.5-beta")]
    [InlineData("2.17.0.5", "2.17.0.6")]
    public void PackageVersion_TheoryCoversPrereleaseAndNumericComparisonBranches(string lower, string higher)
    {
        Assert.True(PackageVersion.Parse(lower).CompareTo(PackageVersion.Parse(higher)) < 0);
    }

    [Theory]
    [InlineData("2.17.0.5-alpha.0")]
    [InlineData("2.17.0.5-1")]
    [InlineData("2.17.0.5-alpha-1")]
    [InlineData("2.17.0.5-alpha.beta.1")]
    public void PackageVersion_AcceptsValidIdentifierAndNormalizedReleaseShapes(string value)
    {
        var parsed = PackageVersion.Parse(value);
        Assert.StartsWith("2.17.0.5", parsed.Value, StringComparison.Ordinal);
    }

    [Fact]
    public void KnowledgeSetIdentity_DistinguishesPackageVersionAndRebuildRevision()
    {
        var first = new KnowledgeSetIdentity(Package, PackageVersion.Parse("2.17.0.4"));
        var second = new KnowledgeSetIdentity(Package, PackageVersion.Parse("2.17.0.10"));
        var rebuild = new KnowledgeSetIdentity(Package, PackageVersion.Parse("2.17.0.4"), "review-1");

        Assert.NotEqual(first, second);
        Assert.NotEqual(first, rebuild);
        Assert.Equal("libpjsua2maui@2.17.0.4", first.Value);
        Assert.Equal("libpjsua2maui@2.17.0.4#review-1", rebuild.Value);
        Assert.NotEqual("2.17.0.4", first.Package.Id);
    }

    [Fact]
    public void Manifest_PreservesVersionAndCopiesCollections()
    {
        var version = PackageVersion.Parse("2.17.0.4");
        var identity = new KnowledgeSetIdentity(Package, version);
        var artifacts = new List<PackageArtifact>
        {
            new(Package, version, "lib/net10.0-android/libpjsua2maui.dll", ArtifactKind.ManagedAssembly, "net10.0-android"),
            new(Package, version, "runtimes/android-arm64/native/libpjsua2maui.so", ArtifactKind.NativeLibrary, runtimeIdentifier: "android-arm64")
        };
        var metadata = new Dictionary<string, string> { ["source"] = "synthetic" };

        var manifest = new KnowledgeManifest(identity, metadata, artifacts);
        artifacts.Clear();
        metadata["source"] = "changed";

        Assert.Equal(version, manifest.PackageVersion);
        Assert.Equal(2, manifest.Artifacts.Count);
        Assert.Equal("synthetic", manifest.Metadata["source"]);
        Assert.Equal(KnowledgeManifestStatus.Incomplete, manifest.Status);
        Assert.IsType<ReadOnlyDictionary<string, string>>(manifest.Metadata);
    }

    [Fact]
    public void Artifact_KeepsTfmAndRidSeparateAndAllowsUnknownPlatformShape()
    {
        var version = PackageVersion.Parse("2.17.0.4");
        var artifact = new PackageArtifact(
            Package,
            version,
            "build/custom/asset.bin",
            ArtifactKind.Other,
            targetFramework: "custom-tfm",
            runtimeIdentifier: "custom-rid");
        var packageOnly = new PackageArtifact(Package, version, "README.md", ArtifactKind.PackageMetadata);

        Assert.Equal("custom-tfm", artifact.TargetFramework);
        Assert.Equal("custom-rid", artifact.RuntimeIdentifier);
        Assert.Null(packageOnly.TargetFramework);
        Assert.Null(packageOnly.RuntimeIdentifier);
    }

    [Theory]
    [InlineData("lib/asset.dll")]
    [InlineData("ref/net10.0/foo.dll")]
    [InlineData("runtimes/linux-x64/native/libfoo.so")]
    [InlineData("runtimes/win-x64/native/foo.dll")]
    public void Artifact_AcceptsCanonicalPackagePaths(string path)
    {
        var artifact = new PackageArtifact(Package, PackageVersion.Parse("2.17.0.4"), path, ArtifactKind.Other);

        Assert.Equal(path, artifact.RelativePath);
    }

    [Theory]
    [InlineData("../outside.dll")]
    [InlineData("./asset.dll")]
    [InlineData("lib/../asset.dll")]
    [InlineData("lib//asset.dll")]
    [InlineData("lib/./asset.dll")]
    [InlineData("C:/asset.dll")]
    [InlineData(@"C:\asset.dll")]
    [InlineData("/absolute/path.dll")]
    [InlineData(@"\\server\share\asset.dll")]
    public void Artifact_RejectsUnsafeOrNonCanonicalPaths(string path)
    {
        Assert.Throws<ArgumentException>(() => new PackageArtifact(Package, PackageVersion.Parse("2.17.0.4"), path, ArtifactKind.Other));
    }

    [Fact]
    public void Artifact_RejectsMissingPathAndIndexedSetRejectsMissingManifest()
    {
        var version = PackageVersion.Parse("2.17.0.4");
        var identity = new KnowledgeSetIdentity(Package, version);
        Assert.Throws<ArgumentNullException>(() => new PackageArtifact(Package, version, null!, ArtifactKind.Other));
        Assert.Throws<ArgumentException>(() => new PackageArtifact(Package, version, " ", ArtifactKind.Other));
        Assert.Throws<ArgumentNullException>(() => new IndexedKnowledgeSet(null!, [], [], [], []));
        Assert.Throws<ArgumentNullException>(() => new PackageAcquisitionOptions(null!));
        Assert.Throws<ArgumentException>(() => new PackageAcquisitionOptions(" "));
    }

    [Fact]
    public void Documentation_AllowsRawMemberContentAndRequiresXmlArtifactFromSameVersion()
    {
        var version = PackageVersion.Parse("2.17.0.4");
        var xml = new PackageArtifact(Package, version, "lib/net10.0-android/libpjsua2maui.xml", ArtifactKind.XmlDocumentation, "net10.0-android");
        var documentation = new DocumentationRecord(Package, version, "M:Example.Call", "text <param name=\"value\">value</param>", xml, "net10.0-android");

        Assert.Contains("<param", documentation.Content);
        Assert.Equal("net10.0-android", documentation.TargetFramework);
    }

    [Fact]
    public void ProvenanceAndRelationship_CanRepresentAnUnresolvedPartialMapping()
    {
        var version = PackageVersion.Parse("2.17.0.4");
        var identity = new KnowledgeSetIdentity(Package, version);
        var symbol = new SymbolIdentity("Example.Call", SymbolKind.Method);
        var provenance = new FactProvenance(identity, ProvenanceKind.PackageArtifact, "lib/net10.0-android/libpjsua2maui.dll", symbol, limitations: "Native mapping not established.");
        var relationship = new KnowledgeRelationship(identity, "managed-to-native", symbol.QualifiedName, provenance: provenance);

        Assert.Null(relationship.Target);
        Assert.Equal(symbol, provenance.Symbol);
        Assert.Contains("not established", provenance.Limitations);
    }

    [Fact]
    public void Models_RejectProvenanceFromAnotherPackageVersion()
    {
        var version = PackageVersion.Parse("2.17.0.4");
        var otherVersion = PackageVersion.Parse("2.17.0.10");
        var identity = new KnowledgeSetIdentity(Package, otherVersion);

        Assert.Throws<ArgumentException>(() => new PackageArtifact(Package, version, "lib/a.dll", ArtifactKind.ManagedAssembly,
            provenance: new FactProvenance(identity, ProvenanceKind.PackageArtifact, "lib/a.dll")));
    }

    [Fact]
    public void DocumentationAssociation_RejectsDifferentTfms()
    {
        var version = PackageVersion.Parse("2.17.0.4");
        var assembly = new PackageArtifact(Package, version, "lib/a.dll", ArtifactKind.ManagedAssembly, "net10.0-android");
        var xml = new PackageArtifact(Package, version, "lib/a.xml", ArtifactKind.XmlDocumentation, "net10.0-ios");
        var symbol = new SymbolRepresentation(Package, version, new SymbolIdentity("Example.Type", SymbolKind.Class), "net10.0-android", assembly);
        var documentation = new DocumentationRecord(Package, version, "T:Example.Type", "content", xml, "net10.0-ios");

        Assert.Throws<ArgumentException>(() => new DocumentationAssociation(documentation, symbol));
        var otherIdentity = new KnowledgeSetIdentity(Package, version, "other");
        Assert.Throws<ArgumentException>(() => new DocumentationAssociation(documentation, symbol,
            new FactProvenance(otherIdentity, ProvenanceKind.PackageArtifact, xml.RelativePath)));
        var otherPackage = new PackageIdentity("other");
        var otherXml = new PackageArtifact(otherPackage, version, "lib/a.xml", ArtifactKind.XmlDocumentation, "net10.0-android");
        var otherDocumentation = new DocumentationRecord(otherPackage, version, "T:Example.Type", "content", otherXml, "net10.0-android");
        Assert.Throws<ArgumentException>(() => new DocumentationAssociation(otherDocumentation, symbol));
        var otherVersion = PackageVersion.Parse("2.17.0.10");
        var otherVersionXml = new PackageArtifact(Package, otherVersion, "lib/a.xml", ArtifactKind.XmlDocumentation, "net10.0-android");
        var otherVersionDocumentation = new DocumentationRecord(Package, otherVersion, "T:Example.Type", "content", otherVersionXml, "net10.0-android");
        Assert.Throws<ArgumentException>(() => new DocumentationAssociation(otherVersionDocumentation, symbol));
        var matchingXml = new PackageArtifact(Package, version, "lib/matching.xml", ArtifactKind.XmlDocumentation, "net10.0-android");
        var matchingDocumentation = new DocumentationRecord(Package, version, "T:Example.Type", "content", matchingXml, "net10.0-android");
        Assert.Throws<ArgumentException>(() => new DocumentationAssociation(matchingDocumentation, symbol,
            new FactProvenance(new KnowledgeSetIdentity(otherPackage, version), ProvenanceKind.PackageArtifact, matchingXml.RelativePath)));
    }

    [Fact]
    public void ModelConstructors_RejectMissingRequiredValuesAndInvalidOptionalText()
    {
        var version = PackageVersion.Parse("2.17.0.4");
        var identity = new KnowledgeSetIdentity(Package, version);
        Assert.Throws<ArgumentNullException>(() => new KnowledgeSetIdentity(null!, version));
        Assert.Throws<ArgumentNullException>(() => new KnowledgeSetIdentity(Package, null!));
        Assert.Throws<ArgumentException>(() => new KnowledgeSetIdentity(Package, version, " "));
        Assert.Throws<ArgumentException>(() => new FactProvenance(identity, ProvenanceKind.PackageArtifact, " "));
        Assert.Throws<ArgumentException>(() => new KnowledgeRelationship(identity, " ", "source"));
        Assert.Throws<ArgumentException>(() => new KnowledgeRelationship(identity, "type", " "));
        Assert.Throws<ArgumentException>(() => new PackageArtifact(Package, version, "lib/a.dll", ArtifactKind.Other, " "));
        Assert.Throws<ArgumentException>(() => new PackageArtifact(Package, version, "lib/a.dll", ArtifactKind.Other, runtimeIdentifier: " "));
    }

    [Fact]
    public void CollectionModels_CopyInputsAndRejectInvalidManifestState()
    {
        var version = PackageVersion.Parse("2.17.0.4");
        var identity = new KnowledgeSetIdentity(Package, version);
        var artifact = new PackageArtifact(Package, version, "lib/a.dll", ArtifactKind.ManagedAssembly);
        var indexed = new KnowledgeManifest(identity, artifacts: [artifact], status: KnowledgeManifestStatus.Indexed);

        var symbols = new List<SymbolRepresentation>();
        var set = new IndexedKnowledgeSet(indexed, symbols, [], [], []);
        symbols.Add(new SymbolRepresentation(Package, version, new SymbolIdentity("Example.Type", SymbolKind.Class)));
        Assert.Empty(set.Symbols);
        Assert.Throws<ArgumentException>(() => new IndexedKnowledgeSet(new KnowledgeManifest(identity, artifacts: [artifact]), [], [], [], []));
        Assert.Throws<ArgumentNullException>(() => new IndexedKnowledgeSet(indexed, null!, [], [], []));
        Assert.Throws<ArgumentNullException>(() => new IndexedKnowledgeSet(indexed, [], null!, [], []));
        Assert.Throws<ArgumentNullException>(() => new IndexedKnowledgeSet(indexed, [], [], null!, []));
        Assert.Throws<ArgumentNullException>(() => new IndexedKnowledgeSet(indexed, [], [], [], null!));
    }

    [Fact]
    public void ManifestAndExtractionModels_RejectMismatchedOrNullInputs()
    {
        var version = PackageVersion.Parse("2.17.0.4");
        var other = new PackageIdentity("other");
        var identity = new KnowledgeSetIdentity(Package, version);
        var foreignArtifact = new PackageArtifact(other, version, "lib/a.dll", ArtifactKind.ManagedAssembly);
        Assert.Throws<ArgumentException>(() => new KnowledgeManifest(identity, artifacts: [foreignArtifact]));
        Assert.Throws<ArgumentNullException>(() => new PackageExtractionResult(null!, [], [], [], []));
        Assert.Throws<ArgumentNullException>(() => new PackageExtractionResult(
            new PackageAcquisitionResult("source.nupkg", new KnowledgeManifest(identity), new PackageDocumentationValidation(version, new Dictionary<string, string>(), [], [])),
            null!, [], [], []));
        var acquisition = new PackageAcquisitionResult("source.nupkg", new KnowledgeManifest(identity), new PackageDocumentationValidation(version, new Dictionary<string, string>(), [], []));
        Assert.Throws<ArgumentNullException>(() => new PackageExtractionResult(acquisition, [], null!, [], []));
        Assert.Throws<ArgumentNullException>(() => new PackageExtractionResult(acquisition, [], [], null!, []));
        Assert.Throws<ArgumentNullException>(() => new PackageExtractionResult(acquisition, [], [], [], null!));
        Assert.Empty(new PackageExtractionResult(acquisition, [], [], [], []).Relationships);
    }

    [Fact]
    public void AcquisitionOptions_RequireAbsoluteStorageAndHttpSource()
    {
        Assert.Throws<ArgumentException>(() => new PackageAcquisitionOptions("relative"));
        Assert.Throws<ArgumentException>(() => new PackageAcquisitionOptions(Path.GetTempPath(), new Uri("file:///tmp/package")));
        Assert.Throws<ArgumentException>(() => new PackageAcquisitionOptions(Path.GetTempPath(), new Uri("ftp://example.test/package")));
        var options = new PackageAcquisitionOptions(Path.GetTempPath());
        Assert.Equal("https", options.PackageSource.Scheme);
        Assert.Equal("http", new PackageAcquisitionOptions(Path.GetTempPath(), new Uri("http://example.test/package")).PackageSource.Scheme);
        var version = PackageVersion.Parse("2.17.0.4");
        var identity = new KnowledgeSetIdentity(Package, version);
        Assert.Throws<ArgumentNullException>(() => new PackageAcquisitionResult(null!, new KnowledgeManifest(identity), new PackageDocumentationValidation(version, new Dictionary<string, string>(), [], [])));
        Assert.Throws<ArgumentNullException>(() => new PackageAcquisitionResult("source.nupkg", null!, new PackageDocumentationValidation(version, new Dictionary<string, string>(), [], [])));
        Assert.Throws<ArgumentNullException>(() => new PackageAcquisitionResult("source.nupkg", new KnowledgeManifest(identity), null!));
        Assert.Throws<ArgumentNullException>(() => new PackageDocumentationValidation(null!, new Dictionary<string, string>(), [], []));
        Assert.Throws<ArgumentNullException>(() => new PackageDocumentationValidation(version, null!, [], []));
        Assert.False(new PackageDocumentationValidation(version, new Dictionary<string, string>(), [], []).IsValid);
    }

    [Fact]
    public void KnowledgeModels_RejectNullRequiredDependencies()
    {
        var version = PackageVersion.Parse("2.17.0.4");
        var identity = new KnowledgeSetIdentity(Package, version);
        var artifact = new PackageArtifact(Package, version, "lib/a.dll", ArtifactKind.ManagedAssembly);
        var xml = new PackageArtifact(Package, version, "lib/a.xml", ArtifactKind.XmlDocumentation);
        var symbol = new SymbolRepresentation(Package, version, new SymbolIdentity("Example.Type", SymbolKind.Class));
        var documentation = new DocumentationRecord(Package, version, "T:Example.Type", "content", xml);
        Assert.Throws<ArgumentNullException>(() => new PackageArtifact(null!, version, "lib/a.dll", ArtifactKind.Other));
        Assert.Throws<ArgumentNullException>(() => new PackageArtifact(Package, null!, "lib/a.dll", ArtifactKind.Other));
        Assert.Throws<ArgumentNullException>(() => new PackageArtifact(Package, version, null!, ArtifactKind.Other));
        Assert.Throws<ArgumentNullException>(() => new SymbolRepresentation(null!, version, symbol.Identity));
        Assert.Throws<ArgumentNullException>(() => new SymbolRepresentation(Package, null!, symbol.Identity));
        Assert.Throws<ArgumentNullException>(() => new SymbolRepresentation(Package, version, null!));
        Assert.Throws<ArgumentNullException>(() => new DocumentationRecord(null!, version, "T:X", "content", xml));
        Assert.Throws<ArgumentNullException>(() => new DocumentationRecord(Package, null!, "T:X", "content", xml));
        Assert.Throws<ArgumentException>(() => new DocumentationRecord(Package, version, " ", "content", xml));
        Assert.Throws<ArgumentNullException>(() => new DocumentationRecord(Package, version, "T:X", "content", null!));
        Assert.Throws<ArgumentNullException>(() => new DocumentationAssociation(null!, symbol));
        Assert.Throws<ArgumentNullException>(() => new DocumentationAssociation(documentation, null!));
        Assert.Throws<ArgumentNullException>(() => new KnowledgeRelationship(null!, "type", "source"));
        Assert.Throws<ArgumentNullException>(() => new FactProvenance(null!, ProvenanceKind.PackageArtifact));
        Assert.Equal("lib/a.dll", artifact.RelativePath);
        Assert.Equal("T:Example.Type", documentation.MemberId);
        Assert.Equal(identity, new KnowledgeSetIdentity(Package, version));
        var foreignPackageArtifact = new PackageArtifact(new PackageIdentity("other"), version, "lib/a.xml", ArtifactKind.XmlDocumentation);
        Assert.Throws<ArgumentException>(() => new DocumentationRecord(Package, version, "T:X", "content", foreignPackageArtifact));
        var foreignVersionArtifact = new PackageArtifact(Package, PackageVersion.Parse("2.17.0.10"), "lib/a.xml", ArtifactKind.XmlDocumentation);
        Assert.Throws<ArgumentException>(() => new DocumentationRecord(Package, version, "T:X", "content", foreignVersionArtifact));
        var wrongKindArtifact = new PackageArtifact(Package, version, "lib/a.dll", ArtifactKind.ManagedAssembly);
        Assert.Throws<ArgumentException>(() => new DocumentationRecord(Package, version, "T:X", "content", wrongKindArtifact));
        Assert.Throws<ArgumentException>(() => new DocumentationRecord(Package, version, "T:X", "content", xml,
            provenance: new FactProvenance(new KnowledgeSetIdentity(Package, PackageVersion.Parse("2.17.0.10")), ProvenanceKind.PackageArtifact, xml.RelativePath)));
        Assert.Throws<ArgumentException>(() => new DocumentationRecord(Package, version, "T:X", "content", xml,
            provenance: new FactProvenance(new KnowledgeSetIdentity(new PackageIdentity("other"), version), ProvenanceKind.PackageArtifact, xml.RelativePath)));
        Assert.Throws<ArgumentException>(() => new KnowledgeRelationship(identity, "type", "source",
            provenance: new FactProvenance(new KnowledgeSetIdentity(Package, PackageVersion.Parse("2.17.0.10")), ProvenanceKind.PackageArtifact, artifact.RelativePath)));
    }

    [Fact]
    public void SymbolRepresentation_RejectsArtifactFromAnotherPackageVersion()
    {
        var firstVersion = PackageVersion.Parse("2.17.0.4");
        var secondVersion = PackageVersion.Parse("2.17.0.10");
        var artifact = new PackageArtifact(Package, firstVersion, "lib/a.dll", ArtifactKind.ManagedAssembly);

        Assert.Throws<ArgumentException>(() => new SymbolRepresentation(
            Package,
            secondVersion,
            new SymbolIdentity("Example.Type", SymbolKind.Class),
            sourceArtifact: artifact));

        Assert.Throws<ArgumentException>(() => new SymbolRepresentation(
            Package,
            firstVersion,
            new SymbolIdentity("Example.Type", SymbolKind.Class),
            provenance: new FactProvenance(new KnowledgeSetIdentity(new PackageIdentity("other"), firstVersion), ProvenanceKind.PackageArtifact, artifact.RelativePath)));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("net10.0", "net10.0")]
    [InlineData("net10.0-ios", "net10.0-android")]
    public void Models_ExerciseOptionalFrameworkAndProvenanceShapes(string? documentationTfm, string? symbolTfm)
    {
        var version = PackageVersion.Parse("2.17.0.5");
        var identity = new KnowledgeSetIdentity(Package, version, "model-theory");
        var assembly = new PackageArtifact(Package, version, "lib/a.dll", ArtifactKind.ManagedAssembly, symbolTfm);
        var xml = new PackageArtifact(Package, version, "lib/a.xml", ArtifactKind.XmlDocumentation, documentationTfm);
        var symbol = new SymbolRepresentation(Package, version, new SymbolIdentity("Example.Type", SymbolKind.Class), symbolTfm, assembly,
            parameters: [new SymbolParameter("value", "System.String", true)]);
        var documentation = new DocumentationRecord(Package, version, "T:Example.Type", "content", xml, documentationTfm,
            new FactProvenance(identity, ProvenanceKind.PackageArtifact, xml.RelativePath));

        if (documentationTfm == symbolTfm)
        {
            var association = new DocumentationAssociation(documentation, symbol,
                new FactProvenance(identity, ProvenanceKind.PackageArtifact, xml.RelativePath, symbol.Identity));
            Assert.Equal(symbol.Identity, association.Symbol.Identity);
        }
        else
        {
            Assert.Throws<ArgumentException>(() => new DocumentationAssociation(documentation, symbol));
        }

        var relationship = new KnowledgeRelationship(identity, "managed-to-native", "Example.Type", " ",
            new FactProvenance(identity, ProvenanceKind.PackageArtifact, assembly.RelativePath));
        Assert.Null(relationship.Target);
        Assert.Equal("model-theory", identity.Revision);
    }
}
