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
        var provenEmpty = new PackageSymbolInventory([], new MetadataEnumerationCoverage
        {
            ExpectedManagedAssemblies = 0,
            InspectedManagedAssemblies = 0,
            SymbolEnumerationCompleted = true
        });

        Assert.False(inventory.Completeness.InventoryComplete);
        Assert.Equal(100m, empty.Completeness.TypeInspectionPercentage);
        Assert.True(provenEmpty.Completeness.InventoryComplete);
        Assert.True(provenEmpty.Completeness.DiscoveryDenominatorProven);
        Assert.Equal(100m, provenEmpty.Completeness.DiscoveryCompletenessPercentage);
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
        var generatedSource = new PackageArtifact(package, version, "src/Platforms/Windows/pjsua2maui/pjsua2/Account.cs", ArtifactKind.ManagedSource, "net10.0");
        var wrapper = new SymbolRepresentation(package, version, new SymbolIdentity("N.Account", SymbolKind.Class), "net10.0", generatedSource, declaringType: "N.Account");
        var acquisition = new PackageAcquisitionResult("source.nupkg", new KnowledgeManifest(new KnowledgeSetIdentity(package, version), new Dictionary<string, string>(), [assembly], KnowledgeManifestStatus.Validated), new PackageDocumentationValidation(version, new Dictionary<string, string>(), [], []));
        var generatedInventory = PackageSymbolInventoryBuilder.Build(new PackageExtractionResult(acquisition, [generated, pinvoke], [], [], []));
        Assert.Equal(SymbolOrigin.SwigOpaqueType, generatedInventory.Items.Single(item => item.Symbol == generated).Origin);
        Assert.Equal(SymbolOrigin.GeneratedHelper, generatedInventory.Items.Single(item => item.Symbol == pinvoke).Origin);
        var wrapperInventory = PackageSymbolInventoryBuilder.Build(new PackageExtractionResult(acquisition, [wrapper], [], [], []));
        Assert.Equal(SymbolOrigin.SwigManagedWrapper, wrapperInventory.Items.Single().Origin);
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
        Assert.Equal(1, first.Inventory.Items.Count(item => item.Symbol.Identity.QualifiedName.Contains("SignatureFixture`1", StringComparison.Ordinal) && !item.Symbol.Identity.QualifiedName.Contains(".Nested", StringComparison.Ordinal) && item.Symbol.Identity.Kind == SymbolKind.Class));
        var fixture = first.Inventory.Items.Single(item => item.Symbol.Identity.QualifiedName.Contains("SignatureFixture`1", StringComparison.Ordinal) && !item.Symbol.Identity.QualifiedName.Contains(".Nested", StringComparison.Ordinal) && item.Symbol.Identity.Kind == SymbolKind.Class);
        Assert.Equal(2, fixture.Evidence.Count);
        Assert.All(fixture.Evidence, evidence => Assert.Equal(EvidenceAssociationStatus.Proven, evidence.SourceStatus));
        Assert.All(fixture.Evidence, evidence => Assert.NotNull(evidence.SourceProvenance));
        Assert.Equal(first.Inventory.Items.Sum(item => item.Evidence.Count), first.Inventory.Counters.TotalTfmEvidence);
        Assert.True(first.Inventory.Completeness.SourceEvidenceCoveragePercentage < 100m);
        Assert.Equal(0m, first.Inventory.Completeness.XmlDocumentationCoveragePercentage);
        Assert.True(first.Inventory.Completeness.DiscoveryDenominatorProven);
        Assert.Equal(100m, first.Inventory.Completeness.DiscoveryCompletenessPercentage);
        Assert.Contains(first.Inventory.Items, item => item.Symbol.Identity.QualifiedName.Contains(".Nested", StringComparison.Ordinal));
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

    [Fact]
    public void Build_DoesNotApplyUntypedSourceToEveryTfmAndReportsAmbiguity()
    {
        var package = new PackageIdentity("libpjsua2maui");
        var version = PackageVersion.Parse("2.17.0.5");
        var assemblyAndroid = new PackageArtifact(package, version, "lib/net10.0-android36.0/libpjsua2maui.dll", ArtifactKind.ManagedAssembly, "net10.0-android36.0");
        var assemblyIos = new PackageArtifact(package, version, "lib/net10.0-ios26.0/libpjsua2maui.dll", ArtifactKind.ManagedAssembly, "net10.0-ios26.0");
        var sourceAndroid = new PackageArtifact(package, version, "src/Platforms/Android/SignatureFixture.cs", ArtifactKind.ManagedSource);
        var sourceIos = new PackageArtifact(package, version, "src/Platforms/iOS/SignatureFixture.cs", ArtifactKind.ManagedSource);
        var sourceDuplicate = new PackageArtifact(package, version, "src/Platforms/Android/legacy/SignatureFixture.cs", ArtifactKind.ManagedSource);
        var xmlAndroid = new PackageArtifact(package, version, "lib/net10.0-android36.0/libpjsua2maui.xml", ArtifactKind.XmlDocumentation, "net10.0-android36.0");
        var manifest = new KnowledgeManifest(new KnowledgeSetIdentity(package, version, "synthetic"), artifacts: [assemblyAndroid, assemblyIos, sourceAndroid, sourceIos, sourceDuplicate, xmlAndroid], status: KnowledgeManifestStatus.Validated);
        var acquisition = new PackageAcquisitionResult("synthetic.nupkg", manifest, new PackageDocumentationValidation(version, new Dictionary<string, string>(), [], []));
        var symbolAndroid = new SymbolRepresentation(package, version, new SymbolIdentity("Example.SignatureFixture", SymbolKind.Class), "net10.0-android36.0", assemblyAndroid, "T:Example.SignatureFixture", declaringType: "Example.SignatureFixture");
        var symbolIos = new SymbolRepresentation(package, version, new SymbolIdentity("Example.SignatureFixture", SymbolKind.Class), "net10.0-ios26.0", assemblyIos, "T:Example.SignatureFixture", declaringType: "Example.SignatureFixture");
        var documentation = new DocumentationRecord(package, version, "T:Example.SignatureFixture", "documented", xmlAndroid, "net10.0-android36.0");

        var inventory = PackageSymbolInventoryBuilder.Build(new PackageExtractionResult(acquisition, [symbolAndroid, symbolIos], [documentation], [], []));
        var item = Assert.Single(inventory.Items);

        Assert.Equal(InspectionStatus.Partial, item.Status);
        Assert.Equal(EvidenceAssociationStatus.Ambiguous, item.Evidence.Single(evidence => evidence.TargetFramework == "net10.0-android36.0").SourceStatus);
        Assert.Equal(EvidenceAssociationStatus.Proven, item.Evidence.Single(evidence => evidence.TargetFramework == "net10.0-ios26.0").SourceStatus);
        Assert.Equal(EvidenceAssociationStatus.Proven, item.Evidence.Single(evidence => evidence.TargetFramework == "net10.0-android36.0").XmlStatus);
        Assert.Equal(EvidenceAssociationStatus.Missing, item.Evidence.Single(evidence => evidence.TargetFramework == "net10.0-ios26.0").XmlStatus);
        Assert.Equal(50m, inventory.Completeness.XmlDocumentationCoveragePercentage);
        Assert.Equal(0m, inventory.Completeness.TfmEvidenceCompletenessPercentage);
    }

    [Fact]
    public void Build_AssociatesExplicitAndRemainingPlatformSourceArtifacts()
    {
        var package = new PackageIdentity("libpjsua2maui");
        var version = PackageVersion.Parse("2.17.0.5");
        var targetFrameworks = new[] { "net10.0-maccatalyst", "net10.0-windows10.0.19041.0" };
        var artifacts = targetFrameworks.Select(tfm => new PackageArtifact(package, version, $"lib/{tfm}/libpjsua2maui.dll", ArtifactKind.ManagedAssembly, tfm)).ToList();
        artifacts.Add(new PackageArtifact(package, version, "src/Platforms/MacCatalyst/MacFixture.cs", ArtifactKind.ManagedSource));
        artifacts.Add(new PackageArtifact(package, version, "src/Platforms/Windows/WindowsFixture.cs", ArtifactKind.ManagedSource));
        artifacts.Add(new PackageArtifact(package, version, "src/Explicit/ExplicitFixture.cs", ArtifactKind.ManagedSource, targetFrameworks[0]));
        artifacts.Add(new PackageArtifact(package, version, "src/Explicit/ExplicitFixture.cs", ArtifactKind.ManagedSource, "other-tfm"));
        var manifest = new KnowledgeManifest(new KnowledgeSetIdentity(package, version, "platform-synthetic"), artifacts: artifacts, status: KnowledgeManifestStatus.Validated);
        var acquisition = new PackageAcquisitionResult("synthetic.nupkg", manifest, new PackageDocumentationValidation(version, new Dictionary<string, string>(), [], []));
        var symbols = new[]
        {
            new SymbolRepresentation(package, version, new SymbolIdentity("MacFixture", SymbolKind.Class), targetFrameworks[0], artifacts[0], declaringType: "MacFixture"),
            new SymbolRepresentation(package, version, new SymbolIdentity("WindowsFixture", SymbolKind.Class), targetFrameworks[1], artifacts[1], declaringType: "WindowsFixture"),
            new SymbolRepresentation(package, version, new SymbolIdentity("ExplicitFixture", SymbolKind.Class), targetFrameworks[1], artifacts[1], declaringType: "ExplicitFixture")
        };

        var inventory = PackageSymbolInventoryBuilder.Build(new PackageExtractionResult(acquisition, symbols, [], [], []));

        Assert.Equal(EvidenceAssociationStatus.Proven, inventory.Items.Single(item => item.Symbol.Identity.QualifiedName == "MacFixture").Evidence.Single().SourceStatus);
        Assert.Equal(EvidenceAssociationStatus.Proven, inventory.Items.Single(item => item.Symbol.Identity.QualifiedName == "WindowsFixture").Evidence.Single().SourceStatus);
        Assert.Equal(EvidenceAssociationStatus.Ambiguous, inventory.Items.Single(item => item.Symbol.Identity.QualifiedName == "ExplicitFixture").Evidence.Single().SourceStatus);
    }

    [Fact]
    public void Build_AllowsUnqualifiedSourceOnlyForTheSingleManagedTfm()
    {
        var package = new PackageIdentity("libpjsua2maui");
        var version = PackageVersion.Parse("2.17.0.5");
        var assembly = new PackageArtifact(package, version, "lib/net10.0/libpjsua2maui.dll", ArtifactKind.ManagedAssembly, "net10.0");
        var source = new PackageArtifact(package, version, "src/Shared/SingleFixture.cs", ArtifactKind.ManagedSource);
        var manifest = new KnowledgeManifest(new KnowledgeSetIdentity(package, version, "single-tfm-synthetic"), artifacts: [assembly, source], status: KnowledgeManifestStatus.Validated);
        var acquisition = new PackageAcquisitionResult("synthetic.nupkg", manifest, new PackageDocumentationValidation(version, new Dictionary<string, string>(), [], []));
        var symbol = new SymbolRepresentation(package, version, new SymbolIdentity("SingleFixture", SymbolKind.Class), "net10.0", assembly, declaringType: "SingleFixture");

        var item = Assert.Single(PackageSymbolInventoryBuilder.Build(new PackageExtractionResult(acquisition, [symbol], [], [], [])).Items);

        Assert.Equal(EvidenceAssociationStatus.Proven, item.Evidence.Single().SourceStatus);
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

            const string androidSourcePath = "src/Platforms/Android/pjsua2/SignatureFixture.cs";
            AddEntry(archive, androidSourcePath, Encoding.UTF8.GetBytes("public class SignatureFixture<T> { }") );
            artifacts.Add(new PackageArtifact(package, version, androidSourcePath, ArtifactKind.ManagedSource));
            const string iosSourcePath = "src/Platforms/iOS/pjsua2/SignatureFixture.cs";
            AddEntry(archive, iosSourcePath, Encoding.UTF8.GetBytes("public class SignatureFixture<T> { }") );
            artifacts.Add(new PackageArtifact(package, version, iosSourcePath, ArtifactKind.ManagedSource));
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
