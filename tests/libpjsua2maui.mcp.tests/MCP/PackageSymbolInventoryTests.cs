using System.IO.Compression;
using System.Text;
using libpjsua2maui.mcp.Knowledge;
using libpjsua2maui.mcp.tests.MCP.TestSupport;

namespace libpjsua2maui.mcp.tests.MCP;

public sealed class PackageSymbolInventoryTests
{
    [Fact]
    public void InventoryModels_ReportEmptyAndNotInspectedEvidenceDeterministically()
    {
        var package = new PackageIdentity("libpjsua2maui");
        var version = PackageVersion.Parse("2.17.0.5");
        var symbol = new SymbolRepresentation(package, version, new SymbolIdentity("Example.Type", SymbolKind.Class));
        var item = new PackageInventoryItem(package, version, "libpjsua2maui", symbol, InspectionStatus.NotInspected, SymbolOrigin.UnknownOrigin, null!, null);
        var inventory = new PackageSymbolInventory([item]);
        var empty = new PackageSymbolInventory([]);

        Assert.False(inventory.Completeness.InventoryComplete);
        Assert.Equal(100m, empty.Completeness.TypeInspectionPercentage);
        Assert.Contains("Package: unknown", empty.Completeness.ToText(null, null, []), StringComparison.Ordinal);
        Assert.Empty(empty.TargetFrameworks);
        Assert.Empty(item.Evidence);
        Assert.Empty(new PackageSymbolInventory(null!).Items);

        var identity = new KnowledgeSetIdentity(package, version, "empty-index");
        var manifest = new KnowledgeManifest(identity, new Dictionary<string, string>(), [], KnowledgeManifestStatus.Indexed);
        var indexed = new IndexedKnowledgeSet(manifest, [], [], [], []);
        Assert.Empty(indexed.Inventory.Items);

        var assembly = new PackageArtifact(package, version, "lib/net10.0/libpjsua2maui.dll", ArtifactKind.ManagedAssembly, "net10.0");
        var generated = new SymbolRepresentation(package, version, new SymbolIdentity("N.SWIGTYPE_p_void", SymbolKind.Class), "net10.0", assembly, declaringType: "N.SWIGTYPE_p_void");
        var pinvoke = new SymbolRepresentation(package, version, new SymbolIdentity("N.PINVOKEHelper", SymbolKind.Class), "net10.0", assembly, declaringType: "N.PINVOKEHelper");
        var acquisition = new PackageAcquisitionResult("source.nupkg", new KnowledgeManifest(new KnowledgeSetIdentity(package, version), new Dictionary<string, string>(), [assembly], KnowledgeManifestStatus.Validated), new PackageDocumentationValidation(version, new Dictionary<string, string>(), [], []));
        var generatedInventory = PackageSymbolInventoryBuilder.Build(new PackageExtractionResult(acquisition, [generated, pinvoke], [], [], []));
        Assert.Equal(SymbolOrigin.GeneratedHelper, generatedInventory.Items.Single(item => item.Symbol == generated).Origin);
    }

    [Fact]
    public async Task ExtractAsync_BuildsGlobalInventoryWithoutTfmDoubleCounting()
    {
        using var package = CreatePackage();
        var first = await new PackageKnowledgeExtractor().ExtractAsync(package.Acquisition, TestContext.Current.CancellationToken);
        var second = await new PackageKnowledgeExtractor().ExtractAsync(package.Acquisition, TestContext.Current.CancellationToken);

        Assert.True(first.Inventory.Completeness.InventoryComplete);
        Assert.Equal(first.Inventory.Counters, second.Inventory.Counters);
        Assert.Equal(first.Inventory.Items.Select(item => item.Identity), second.Inventory.Items.Select(item => item.Identity));
        Assert.Equal(first.Inventory.Items.Select(item => item.Status), second.Inventory.Items.Select(item => item.Status));
        Assert.Contains("Version: 2.17.0.5", first.Inventory.TextReport, StringComparison.Ordinal);
        Assert.Equal(["net10.0-android36.0", "net10.0-ios26.0"], first.Inventory.TargetFrameworks);
        Assert.Equal(1, first.Inventory.Items.Count(item => item.Symbol.Identity.QualifiedName.Contains("SignatureFixture`1", StringComparison.Ordinal) && item.Symbol.Identity.Kind == SymbolKind.Class));
        Assert.Equal(2, first.Inventory.Items.Single(item => item.Symbol.Identity.QualifiedName.Contains("SignatureFixture`1", StringComparison.Ordinal) && item.Symbol.Identity.Kind == SymbolKind.Class).Evidence.Count);
    }

    [Fact]
    public async Task ExtractAsync_PreservesOverloadsAndAuditsMissingEvidence()
    {
        using var package = CreatePackage(xml: "<doc><members><member name=\"T:libpjsua2maui.mcp.tests.MCP.TestSupport.SignatureFixture`1\"><remarks>Documented without summary.</remarks></member></members></doc>");
        var result = await new PackageKnowledgeExtractor().ExtractAsync(package.Acquisition, TestContext.Current.CancellationToken);
        var methods = result.Inventory.Items.Where(item => item.Symbol.Identity.Kind == SymbolKind.Method && item.Symbol.Identity.QualifiedName.Contains("Primitive", StringComparison.Ordinal)).ToArray();

        Assert.NotEmpty(methods);
        Assert.Equal(methods.Length, methods.Select(item => item.Identity).Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(result.Inventory.Items, item => item.Status == InspectionStatus.Partial && item.StatusReason!.Contains("XML documentation", StringComparison.Ordinal));
        Assert.Contains(result.Inventory.Items, item => item.Origin == SymbolOrigin.SwigEnum);
    }

    private static TestPackage CreatePackage(string? xml = null)
    {
        var path = Path.Combine(Path.GetTempPath(), $"inventory-{Guid.NewGuid():N}.nupkg");
        var package = new PackageIdentity("libpjsua2maui");
        var version = PackageVersion.Parse("2.17.0.5");
        var tfms = new[] { "net10.0-android36.0", "net10.0-ios26.0" };
        var artifacts = new List<PackageArtifact>();
        using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
        {
            foreach (var tfm in tfms)
            {
                var assemblyPath = $"lib/{tfm}/libpjsua2maui.dll";
                var xmlPath = $"lib/{tfm}/libpjsua2maui.xml";
                AddEntry(archive, assemblyPath, File.ReadAllBytes(typeof(SignatureFixture<>).Assembly.Location));
                AddEntry(archive, xmlPath, Encoding.UTF8.GetBytes(xml ?? "<doc><members /></doc>"));
                artifacts.Add(new PackageArtifact(package, version, assemblyPath, ArtifactKind.ManagedAssembly, tfm));
                artifacts.Add(new PackageArtifact(package, version, xmlPath, ArtifactKind.XmlDocumentation, tfm));
            }

            const string sourcePath = "src/pjsua2/SignatureFixture.cs";
            AddEntry(archive, sourcePath, Encoding.UTF8.GetBytes("public class SignatureFixture<T> { }") );
            artifacts.Add(new PackageArtifact(package, version, sourcePath, ArtifactKind.ManagedSource));
            const string unmatchedSourcePath = "src/other/SignatureFixture.cs";
            AddEntry(archive, unmatchedSourcePath, Encoding.UTF8.GetBytes("public class SignatureFixture<T> { }") );
            artifacts.Add(new PackageArtifact(package, version, unmatchedSourcePath, ArtifactKind.ManagedSource, "other-tfm"));
        }

        var manifest = new KnowledgeManifest(new KnowledgeSetIdentity(package, version, "inventory-tests"), new Dictionary<string, string>(), artifacts, KnowledgeManifestStatus.Validated);
        var acquisition = new PackageAcquisitionResult(path, manifest, new PackageDocumentationValidation(version, new Dictionary<string, string>(), [], []));
        return new TestPackage(path, acquisition);
    }

    private static void AddEntry(ZipArchive archive, string path, byte[] content)
    {
        using var target = archive.CreateEntry(path).Open();
        target.Write(content);
    }

    private sealed class TestPackage(string path, PackageAcquisitionResult acquisition) : IDisposable
    {
        public PackageAcquisitionResult Acquisition { get; } = acquisition;
        public void Dispose() => File.Delete(path);
    }
}
