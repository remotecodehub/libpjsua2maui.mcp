using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using libpjsua2maui.mcp.Knowledge;

namespace libpjsua2maui.mcp.tests.MCP;

public sealed class KnowledgeIndexingAndPersistenceTests
{
    [Fact]
    public async Task IndexAsync_RejectsNullCancellationAndUnvalidatedAcquisition()
    {
        var indexer = new KnowledgeIndexer();
#pragma warning disable xUnit1051
        await Assert.ThrowsAsync<ArgumentNullException>(() => indexer.IndexAsync(null!));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => indexer.IndexAsync(CreateExtraction(), cancellation.Token));

        var extraction = CreateExtraction();
        var manifest = new KnowledgeManifest(extraction.Acquisition.Manifest.Identity, extraction.Acquisition.Manifest.Metadata, extraction.Acquisition.Manifest.Artifacts);
        var invalid = new PackageExtractionResult(
            new PackageAcquisitionResult(extraction.Acquisition.ArtifactPath, manifest, extraction.Acquisition.Documentation),
            extraction.Symbols, extraction.Documentation, extraction.Associations, extraction.UnassociatedDocumentation, extraction.Relationships);
        await Assert.ThrowsAsync<InvalidDataException>(() => indexer.IndexAsync(invalid));
#pragma warning restore xUnit1051
    }

    [Fact]
    public async Task IndexAndPersist_AttachesAndRestoresFactProvenance()
    {
        var indexed = await new KnowledgeIndexer().IndexAsync(CreateExtraction(), TestContext.Current.CancellationToken);
        var symbol = Assert.Single(indexed.Symbols, item => item.TargetFramework == "net10.0-android");
        var documentation = Assert.Single(indexed.Documentation, item => item.MemberId == "M:Example.Call(System.String)");

        Assert.All(indexed.Manifest.Artifacts, artifact => Assert.Equal(indexed.Manifest.Identity, artifact.Provenance!.KnowledgeSet));
        Assert.Equal(symbol.Identity, symbol.Provenance!.Symbol);
        Assert.Equal(documentation.SourceArtifact.RelativePath, documentation.Provenance!.ArtifactPath);
        Assert.Contains("does not by itself", symbol.Provenance.Limitations);

        var path = Path.Combine(Path.GetTempPath(), $"knowledge-provenance-{Guid.NewGuid():N}.json");
        try
        {
            var store = new JsonKnowledgeStore();
            await store.SaveAsync(path, indexed, TestContext.Current.CancellationToken);
            var loaded = await store.LoadAsync(path, TestContext.Current.CancellationToken);
            Assert.Equal(indexed.Manifest.Identity, loaded.Manifest.Identity);
            Assert.Equal(symbol.Provenance, loaded.Symbols.Single(item => item.Identity == symbol.Identity).Provenance);
            Assert.Equal(documentation.Provenance, loaded.Documentation.Single(item => item.MemberId == documentation.MemberId).Provenance);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{")]
    [InlineData("[]")]
    public async Task LoadAsync_RejectsEmptyMalformedAndWrongRootJson(string json)
    {
        await AssertLoadRejectsAsync(json, typeof(InvalidDataException));
    }

    [Theory]
    [InlineData("{\"schemaVersion\":1,\"packageId\":\"other\"}")]
    [InlineData("{\"schemaVersion\":1,\"packageId\":\"libpjsua2maui\",\"packageVersion\":\"2.17.0.4\",\"manifestStatus\":\"1\"}")]
    [InlineData("{\"schemaVersion\":1,\"packageId\":\"libpjsua2maui\",\"packageVersion\":\"2.17.0.4\",\"manifestStatus\":\"Indexed\",\"metadata\":{},\"artifacts\":[]}")]
    public async Task LoadAsync_RejectsInvalidManifestAndMissingCollections(string json)
    {
        await AssertLoadRejectsAsync(json, typeof(InvalidDataException));
    }

    [Theory]
    [InlineData("packageId")]
    [InlineData("packageVersion")]
    [InlineData("artifacts")]
    [InlineData("documentation")]
    [InlineData("unassociatedDocumentation")]
    [InlineData("associations")]
    [InlineData("relationships")]
    [InlineData("metadata")]
    public async Task LoadAsync_RejectsNullRequiredPersistedProperty(string propertyName)
    {
        var indexed = await new KnowledgeIndexer().IndexAsync(CreateExtraction(), TestContext.Current.CancellationToken);
        var path = Path.Combine(Path.GetTempPath(), $"knowledge-null-{Guid.NewGuid():N}.json");
        try
        {
            await new JsonKnowledgeStore().SaveAsync(path, indexed, TestContext.Current.CancellationToken);
            var root = JsonNode.Parse(await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken))!.AsObject();
            root[propertyName] = null;
            await File.WriteAllTextAsync(path, root.ToJsonString(new JsonSerializerOptions { WriteIndented = false }), TestContext.Current.CancellationToken);

            await Assert.ThrowsAsync<InvalidDataException>(() => new JsonKnowledgeStore().LoadAsync(path, TestContext.Current.CancellationToken));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task LoadAsync_RejectsInvalidEnumAndTraversalArtifact()
    {
        var json = """
        {
          "schemaVersion": 1,
          "packageId": "libpjsua2maui",
          "packageVersion": "2.17.0.4",
          "manifestStatus": "Indexed",
          "metadata": {},
          "artifacts": [
            { "path": "lib/../bad.dll", "kind": "ManagedAssembly" }
          ],
          "symbols": [], "documentation": [], "unassociatedDocumentation": [], "associations": [], "relationships": []
        }
        """;

        await AssertLoadRejectsAsync(json, typeof(InvalidDataException));
    }

    [Fact]
    public async Task LoadAsync_RejectsConflictingArtifactIdentity()
    {
        var json = """
        {
          "schemaVersion": 1,
          "packageId": "libpjsua2maui",
          "packageVersion": "2.17.0.4",
          "manifestStatus": "Indexed",
          "metadata": {},
          "artifacts": [
            { "path": "lib/a.dll", "kind": "ManagedAssembly" },
            { "path": "lib/a.dll", "kind": "NativeLibrary" }
          ],
          "symbols": [], "documentation": [], "unassociatedDocumentation": [], "associations": [], "relationships": []
        }
        """;

        await AssertLoadRejectsAsync(json, typeof(Exception));
    }

    [Fact]
    public async Task SaveAsync_HonorsCancellationBeforeCreatingFile()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var path = Path.Combine(Path.GetTempPath(), $"knowledge-cancel-{Guid.NewGuid():N}.json");

        var indexed = await new KnowledgeIndexer().IndexAsync(CreateExtraction(), TestContext.Current.CancellationToken);
        await Assert.ThrowsAsync<OperationCanceledException>(() => new JsonKnowledgeStore().SaveAsync(path, indexed, cancellation.Token));
        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task SaveAsync_CleansTemporaryFileWhenDestinationCannotBeReplaced()
    {
        var directory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), $"knowledge-save-{Guid.NewGuid():N}"));
        directory.Create();
        var indexed = await new KnowledgeIndexer().IndexAsync(CreateExtraction(), TestContext.Current.CancellationToken);
        var destination = Path.Combine(directory.FullName, "destination");
        Directory.CreateDirectory(destination);
        try
        {
            await Assert.ThrowsAnyAsync<Exception>(() => new JsonKnowledgeStore().SaveAsync(destination, indexed, TestContext.Current.CancellationToken));
            Assert.Empty(directory.EnumerateFiles("*.tmp", SearchOption.TopDirectoryOnly));
        }
        finally
        {
            directory.Delete(true);
        }
    }

    [Fact]
    public void PersistenceDtos_RejectInvalidReferencesAndPreserveOptionalShapes()
    {
        var package = new PackageIdentity("libpjsua2maui");
        var version = PackageVersion.Parse("2.17.0.4");
        var identity = new KnowledgeSetIdentity(package, version, "dto");
        var assembly = new PackageArtifact(package, version, "lib/a.dll", ArtifactKind.ManagedAssembly, "net10.0");
        var xml = new PackageArtifact(package, version, "lib/a.xml", ArtifactKind.XmlDocumentation, "net10.0");
        var artifacts = new Dictionary<string, PackageArtifact> { [assembly.RelativePath] = assembly, [xml.RelativePath] = xml };
        var symbol = new SymbolRepresentation(package, version, new SymbolIdentity("Example.Type", SymbolKind.Class), "net10.0", assembly);
        var documentation = new DocumentationRecord(package, version, "T:Example.Type", "content", xml, "net10.0");
        var symbols = new Dictionary<string, SymbolRepresentation> { ["net10.0|Class|Example.Type||" ] = symbol };
        var docs = new Dictionary<string, DocumentationRecord> { ["net10.0|T:Example.Type|lib/a.xml"] = documentation };

        Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.ArtifactDto { Path = "lib/a.dll", Kind = "999" }.ToModel(package, version, identity));
        Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.ArtifactDto
        {
            Path = "lib/a.dll",
            Kind = "ManagedAssembly",
            Provenance = new JsonKnowledgeStore.ProvenanceDto { Kind = "PackageArtifact", ArtifactPath = "lib/mismatch.dll" }
        }.ToModel(package, version, identity));
        Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.SymbolDto { QualifiedName = "Example.Type", Kind = "Class", ArtifactPath = "missing.dll" }.ToModel(package, version, artifacts, identity));
        Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.ParameterDto().ToModel());
        Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.ParameterDto { Name = "value" }.ToModel());
        Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.DocumentationDto { MemberId = "T:Example.Type", Content = "content", ArtifactPath = "lib/a.dll" }.ToModel(package, version, artifacts, identity));
        Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.AssociationDto { DocumentationKey = "missing", SymbolKey = "missing" }.ToModel(docs, symbols, identity));
        Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.RelationshipDto { Type = "type", Source = "source", Provenance = new JsonKnowledgeStore.ProvenanceDto { Kind = "PackageArtifact", ArtifactPath = "missing.dll" } }.ToModel(identity, artifacts, symbols));
        Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.ProvenanceDto { Kind = "999" }.ToModel(identity));
        Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.ProvenanceDto { Kind = "PackageArtifact", SymbolQualifiedName = "Example.Type" }.ToModel(identity));

        var valid = new JsonKnowledgeStore.SymbolDto { QualifiedName = "Example.Type", Kind = "Class", TargetFramework = "net10.0", ArtifactPath = assembly.RelativePath }.ToModel(package, version, artifacts, identity);
        Assert.Equal(symbol.Identity, valid.Identity);
        Assert.Equal("content", new JsonKnowledgeStore.DocumentationDto { MemberId = documentation.MemberId, Content = documentation.Content, TargetFramework = "net10.0", ArtifactPath = xml.RelativePath }.ToModel(package, version, artifacts, identity).Content);
        Assert.Null(new JsonKnowledgeStore.SymbolDto { QualifiedName = "Detached", Kind = "Class" }.ToModel(package, version, artifacts, identity).SourceArtifact);
        Assert.Null(new JsonKnowledgeStore.DocumentationDto { MemberId = documentation.MemberId, Content = documentation.Content, TargetFramework = "net10.0", ArtifactPath = xml.RelativePath }.ToModel(package, version, artifacts, identity).Provenance);
        Assert.Equal(documentation.MemberId, new JsonKnowledgeStore.AssociationDto
        {
            DocumentationKey = docs.Keys.Single(),
            SymbolKey = symbols.Keys.Single()
        }.ToModel(docs, symbols, identity).Documentation.MemberId);
        Assert.Equal("source", new JsonKnowledgeStore.RelationshipDto { Type = "type", Source = "source" }
            .ToModel(identity, artifacts, symbols).Source);
        Assert.Null(new JsonKnowledgeStore.ProvenanceDto { Kind = "PackageArtifact" }.ToModel(identity).Symbol);
        Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.AssociationDto
        {
            DocumentationKey = docs.Keys.Single(),
            SymbolKey = symbols.Keys.Single()
        }.ToModel(
            new Dictionary<string, DocumentationRecord> { [docs.Keys.Single()] = new DocumentationRecord(package, version, documentation.MemberId, documentation.Content, xml, "net10.0-ios") },
            symbols,
            identity));
    }

    [Theory]
    [InlineData("artifact-kind")]
    [InlineData("artifact-path")]
    [InlineData("artifact-rooted")]
    [InlineData("artifact-drive-backslash")]
    [InlineData("artifact-absolute")]
    [InlineData("artifact-unc")]
    [InlineData("artifact-parent-segment")]
    [InlineData("artifact-current-segment")]
    [InlineData("artifact-parent-segment-nested")]
    [InlineData("artifact-backslash")]
    [InlineData("artifact-empty-segment")]
    [InlineData("symbol-kind")]
    [InlineData("symbol-name")]
    [InlineData("symbol-artifact")]
    [InlineData("symbol-tfm")]
    [InlineData("documentation-artifact")]
    [InlineData("documentation-member")]
    [InlineData("documentation-content")]
    [InlineData("association-documentation")]
    [InlineData("association-symbol")]
    [InlineData("relationship-type")]
    [InlineData("relationship-source")]
    public void PersistenceDtos_TheoryRejectsInvalidShape(string scenario)
    {
        var package = new PackageIdentity("libpjsua2maui");
        var version = PackageVersion.Parse("2.17.0.4");
        var identity = new KnowledgeSetIdentity(package, version);
        var assembly = new PackageArtifact(package, version, "lib/a.dll", ArtifactKind.ManagedAssembly, "net10.0");
        var xml = new PackageArtifact(package, version, "lib/a.xml", ArtifactKind.XmlDocumentation, "net10.0");
        var artifacts = new Dictionary<string, PackageArtifact> { [assembly.RelativePath] = assembly, [xml.RelativePath] = xml };
        var symbol = new SymbolRepresentation(package, version, new SymbolIdentity("Example.Type", SymbolKind.Class), "net10.0", assembly);
        var documentation = new DocumentationRecord(package, version, "T:Example.Type", "content", xml, "net10.0");
        var symbols = new Dictionary<string, SymbolRepresentation> { ["symbol"] = symbol };
        var docs = new Dictionary<string, DocumentationRecord> { ["documentation"] = documentation };

        switch (scenario)
        {
            case "artifact-kind": Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.ArtifactDto { Path = "lib/a.dll" }.ToModel(package, version, identity)); break;
            case "artifact-path": Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.ArtifactDto { Kind = "ManagedAssembly" }.ToModel(package, version, identity)); break;
            case "artifact-rooted": Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.ArtifactDto { Path = "C:/a.dll", Kind = "ManagedAssembly" }.ToModel(package, version, identity)); break;
            case "artifact-drive-backslash": Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.ArtifactDto { Path = @"C:\a.dll", Kind = "ManagedAssembly" }.ToModel(package, version, identity)); break;
            case "artifact-absolute": Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.ArtifactDto { Path = "/absolute/a.dll", Kind = "ManagedAssembly" }.ToModel(package, version, identity)); break;
            case "artifact-unc": Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.ArtifactDto { Path = @"\\server\share\a.dll", Kind = "ManagedAssembly" }.ToModel(package, version, identity)); break;
            case "artifact-parent-segment": Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.ArtifactDto { Path = "../a.dll", Kind = "ManagedAssembly" }.ToModel(package, version, identity)); break;
            case "artifact-current-segment": Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.ArtifactDto { Path = "./a.dll", Kind = "ManagedAssembly" }.ToModel(package, version, identity)); break;
            case "artifact-parent-segment-nested": Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.ArtifactDto { Path = "lib/../a.dll", Kind = "ManagedAssembly" }.ToModel(package, version, identity)); break;
            case "artifact-backslash": Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.ArtifactDto { Path = "lib\\a.dll", Kind = "ManagedAssembly" }.ToModel(package, version, identity)); break;
            case "artifact-empty-segment": Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.ArtifactDto { Path = "lib//a.dll", Kind = "ManagedAssembly" }.ToModel(package, version, identity)); break;
            case "symbol-kind": Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.SymbolDto { QualifiedName = "x" }.ToModel(package, version, artifacts, identity)); break;
            case "symbol-name": Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.SymbolDto { Kind = "Class" }.ToModel(package, version, artifacts, identity)); break;
            case "symbol-artifact": Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.SymbolDto { QualifiedName = "x", Kind = "Class", ArtifactPath = "missing" }.ToModel(package, version, artifacts, identity)); break;
            case "symbol-tfm": Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.SymbolDto { QualifiedName = "x", Kind = "Class", ArtifactPath = assembly.RelativePath, TargetFramework = "net10.0-ios" }.ToModel(package, version, artifacts, identity)); break;
            case "documentation-artifact": Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.DocumentationDto { MemberId = "T:x", Content = "x", ArtifactPath = assembly.RelativePath, TargetFramework = "net10.0" }.ToModel(package, version, artifacts, identity)); break;
            case "documentation-member": Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.DocumentationDto { Content = "x", ArtifactPath = xml.RelativePath, TargetFramework = "net10.0" }.ToModel(package, version, artifacts, identity)); break;
            case "documentation-content": Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.DocumentationDto { MemberId = "T:x", ArtifactPath = xml.RelativePath, TargetFramework = "net10.0" }.ToModel(package, version, artifacts, identity)); break;
            case "association-documentation": Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.AssociationDto { SymbolKey = "symbol" }.ToModel(docs, symbols, identity)); break;
            case "association-symbol": Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.AssociationDto { DocumentationKey = "documentation" }.ToModel(docs, symbols, identity)); break;
            case "relationship-type": Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.RelationshipDto { Source = "x" }.ToModel(identity, artifacts, symbols)); break;
            case "relationship-source": Assert.Throws<InvalidDataException>(() => new JsonKnowledgeStore.RelationshipDto { Type = "x" }.ToModel(identity, artifacts, symbols)); break;
        }
    }
    [Fact]
    public async Task IndexAndPersist_PreservesOverloadsRawDocumentationAndPlatformBoundaries()
    {
        var fixture = CreateExtraction();
        var indexed = await new KnowledgeIndexer().IndexAsync(fixture, TestContext.Current.CancellationToken);
        var path = Path.Combine(Path.GetTempPath(), $"knowledge-{Guid.NewGuid():N}.json");
        try
        {
            await new JsonKnowledgeStore().SaveAsync(path, indexed, TestContext.Current.CancellationToken);
            var loaded = await new JsonKnowledgeStore().LoadAsync(path, TestContext.Current.CancellationToken);

            Assert.Equal("2.17.0.4", loaded.Manifest.PackageVersion.Value);
            Assert.Equal("review-1", loaded.Manifest.Identity.Revision);
            Assert.Equal(KnowledgeManifestStatus.Indexed, loaded.Manifest.Status);
            Assert.Equal(2, loaded.Symbols.Count);
            Assert.Contains(loaded.Symbols, symbol => symbol.TargetFramework == "net10.0-android");
            Assert.Contains(loaded.Symbols, symbol => symbol.TargetFramework == "net10.0-ios");
            Assert.Contains("<returns>Raw XML</returns>", loaded.Documentation.Single(documentation => documentation.MemberId == "M:Example.Call(System.String)").Content);
            Assert.Single(loaded.UnassociatedDocumentation);
            Assert.Single(loaded.Associations);
            Assert.Single(loaded.Relationships);
            Assert.Contains("not established", loaded.Relationships[0].Provenance!.Limitations);
            Assert.DoesNotContain(Path.GetDirectoryName(path)!, Encoding.UTF8.GetString(await File.ReadAllBytesAsync(path, TestContext.Current.CancellationToken)));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task SaveAsync_IsDeterministicForEquivalentCollectionOrder()
    {
        var fixture = CreateExtraction(reverse: false);
        var reversed = CreateExtraction(reverse: true);
        var indexer = new KnowledgeIndexer();
        var first = await indexer.IndexAsync(fixture, TestContext.Current.CancellationToken);
        var second = await indexer.IndexAsync(reversed, TestContext.Current.CancellationToken);
        var firstPath = Path.Combine(Path.GetTempPath(), $"knowledge-{Guid.NewGuid():N}.json");
        var secondPath = Path.Combine(Path.GetTempPath(), $"knowledge-{Guid.NewGuid():N}.json");
        try
        {
            var store = new JsonKnowledgeStore();
            await store.SaveAsync(firstPath, first, TestContext.Current.CancellationToken);
            await store.SaveAsync(secondPath, second, TestContext.Current.CancellationToken);
            Assert.Equal(await File.ReadAllBytesAsync(firstPath, TestContext.Current.CancellationToken), await File.ReadAllBytesAsync(secondPath, TestContext.Current.CancellationToken));
        }
        finally
        {
            File.Delete(firstPath);
            File.Delete(secondPath);
        }
    }

    [Fact]
    public async Task LoadAsync_RejectsUnsupportedSchema()
    {
        var path = Path.Combine(Path.GetTempPath(), $"knowledge-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(path, "{\"schemaVersion\":99}", TestContext.Current.CancellationToken);
            var exception = await Assert.ThrowsAsync<InvalidDataException>(() => new JsonKnowledgeStore().LoadAsync(path, TestContext.Current.CancellationToken));
            Assert.Contains("Unsupported knowledge schema version", exception.Message);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static PackageExtractionResult CreateExtraction(bool reverse = false)
    {
        var package = new PackageIdentity("libpjsua2maui");
        var version = PackageVersion.Parse("2.17.0.4");
        var identity = new KnowledgeSetIdentity(package, version, "review-1");
        var androidAssembly = new PackageArtifact(package, version, "lib/net10.0-android/libpjsua2maui.dll", ArtifactKind.ManagedAssembly, "net10.0-android");
        var androidXml = new PackageArtifact(package, version, "lib/net10.0-android/libpjsua2maui.xml", ArtifactKind.XmlDocumentation, "net10.0-android");
        var iosAssembly = new PackageArtifact(package, version, "lib/net10.0-ios/libpjsua2maui.dll", ArtifactKind.ManagedAssembly, "net10.0-ios");
        var manifest = new KnowledgeManifest(identity, new Dictionary<string, string> { ["source"] = "synthetic" }, [iosAssembly, androidXml, androidAssembly], KnowledgeManifestStatus.Validated);
        var acquisition = new PackageAcquisitionResult(Path.Combine(Path.GetTempPath(), "source.nupkg"), manifest, new PackageDocumentationValidation(version, new Dictionary<string, string>(), [], []));
        var android = new SymbolRepresentation(package, version, new SymbolIdentity("Example.Call(System.String)", SymbolKind.Method), "net10.0-android", androidAssembly, "M:Example.Call(System.String)", "Example.Call(System.String)", [new SymbolParameter("value", "System.String")], "System.Void");
        var ios = new SymbolRepresentation(package, version, new SymbolIdentity("Example.Call(System.Int32)", SymbolKind.Method), "net10.0-ios", iosAssembly, "M:Example.Call(System.Int32)", "Example.Call(System.Int32)", [new SymbolParameter("value", "System.Int32")], "System.Void");
        var documentation = new DocumentationRecord(package, version, "M:Example.Call(System.String)", "<returns>Raw XML</returns><param name=\"value\">Value</param>", androidXml, "net10.0-android");
        var unassociated = new DocumentationRecord(package, version, "M:Unknown", "<remarks>Unassociated</remarks>", androidXml, "net10.0-android");
        var symbols = reverse ? new[] { android, ios } : new[] { ios, android };
        var docs = reverse ? new[] { unassociated, documentation } : new[] { documentation, unassociated };
        var association = new DocumentationAssociation(documentation, android);
        var relationship = new KnowledgeRelationship(identity, "managed-to-native", android.Identity.QualifiedName,
            provenance: new FactProvenance(identity, ProvenanceKind.PackageArtifact, androidAssembly.RelativePath, android.Identity,
                limitations: "Native mapping is not established."));
        return new PackageExtractionResult(acquisition, symbols, docs, [association], [unassociated], [relationship]);
    }

    private static async Task AssertLoadRejectsAsync(string json, Type exceptionType)
    {
        var path = Path.Combine(Path.GetTempPath(), $"knowledge-invalid-{Guid.NewGuid():N}.json");
        try
        {
            await File.WriteAllTextAsync(path, json, TestContext.Current.CancellationToken);
            var exception = await Assert.ThrowsAnyAsync<Exception>(() => new JsonKnowledgeStore().LoadAsync(path, TestContext.Current.CancellationToken));
            Assert.True(exceptionType.IsAssignableFrom(exception.GetType()), $"Expected {exceptionType}, got {exception.GetType()}.");
        }
        finally
        {
            File.Delete(path);
        }
    }
}
