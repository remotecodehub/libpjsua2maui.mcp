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
    public void PackageVersion_RejectsMalformedValues(string value)
    {
        Assert.Throws<FormatException>(() => PackageVersion.Parse(value));
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
    }
}
