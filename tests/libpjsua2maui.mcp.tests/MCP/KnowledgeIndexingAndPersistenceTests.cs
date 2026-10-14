using System.Text;
using libpjsua2maui.mcp.Knowledge;

namespace libpjsua2maui.mcp.tests.MCP;

public sealed class KnowledgeIndexingAndPersistenceTests
{
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
        return new PackageExtractionResult(acquisition, symbols, docs, [association], [unassociated]);
    }
}
