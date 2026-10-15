using System.IO.Compression;
using System.Text;
using libpjsua2maui.mcp.Knowledge;

namespace libpjsua2maui.mcp.tests.MCP;

public sealed class PackageKnowledgeExtractorTests
{
    [Fact]
    public async Task ExtractAsync_ReadsMetadataSignatureShapesAndPublicMemberKinds()
    {
        using var package = CreatePackage("net10.0-android", typeof(TestSupport.SignatureFixture<>).Assembly.Location, "<doc><members /></doc>");

        var result = await new PackageKnowledgeExtractor().ExtractAsync(package.Acquisition, TestContext.Current.CancellationToken);

        Assert.Contains(result.Symbols, symbol => symbol.Identity.Kind == SymbolKind.Interface && symbol.Identity.QualifiedName.Contains("ISignatureFixture", StringComparison.Ordinal));
        Assert.Contains(result.Symbols, symbol => symbol.Identity.Kind == SymbolKind.Enum && symbol.Identity.QualifiedName.Contains("SignatureFixtureEnum", StringComparison.Ordinal));
        Assert.Contains(result.Symbols, symbol => symbol.Identity.Kind == SymbolKind.Struct && symbol.Identity.QualifiedName.Contains("SignatureFixtureStruct", StringComparison.Ordinal));
        Assert.Contains(result.Symbols, symbol => symbol.Identity.Kind == SymbolKind.Delegate && symbol.Identity.QualifiedName.Contains("SignatureFixtureDelegate", StringComparison.Ordinal));
        Assert.Contains(result.Symbols, symbol => symbol.Identity.Kind == SymbolKind.Event && symbol.Identity.QualifiedName.Contains("Changed", StringComparison.Ordinal));
        Assert.Contains(result.Symbols, symbol => symbol.Identity.Kind == SymbolKind.Field && symbol.Identity.QualifiedName.Contains("Constant", StringComparison.Ordinal));
        Assert.Contains(result.Symbols, symbol => symbol.Signature?.Contains("System.Boolean", StringComparison.Ordinal) == true);
        Assert.Contains(result.Symbols, symbol => symbol.Signature?.Contains("System.String[]", StringComparison.Ordinal) == true);
        Assert.Contains(result.Symbols, symbol => symbol.Signature?.Contains("System.Int32[,]", StringComparison.Ordinal) == true);
        Assert.Contains(result.Symbols, symbol => symbol.Signature?.Contains("System.Int32[,,]", StringComparison.Ordinal) == true);
        Assert.Contains(result.Symbols, symbol => symbol.Identity.QualifiedName.Contains(".Generic(", StringComparison.Ordinal));
        Assert.Contains(result.Symbols, symbol => symbol.Signature?.Contains("System.Int32&", StringComparison.Ordinal) == true);
        Assert.Contains(result.Symbols, symbol => symbol.Signature?.Contains("System.IntPtr", StringComparison.Ordinal) == true);
        Assert.Contains(result.Symbols, symbol => symbol.Signature?.Contains("!!", StringComparison.Ordinal) == true);
    }

    [Fact]
    public async Task ExtractAsync_PreservesMemberContentWithoutSummaryAndAssociatesById()
    {
        using var package = CreatePackage("net10.0-android", typeof(PackageVersion).Assembly.Location,
            """
            <doc><members>
              <member name="M:libpjsua2maui.mcp.Knowledge.PackageVersion.Parse(System.String)">
                Direct member text.
                <param name="value">The <paramref name="value"/> to parse.</param>
                <returns>A <see cref="T:libpjsua2maui.mcp.Knowledge.PackageVersion"/>.</returns>
                <remarks>Additional remarks.</remarks>
                <exception cref="T:System.ArgumentException">Bad input.</exception>
                <example><code>PackageVersion.Parse(value)</code></example>
              </member>
              <member name="M:libpjsua2maui.mcp.Knowledge.PackageVersion.ToString">
                <returns>Text.</returns>
              </member>
              <member name="M:libpjsua2maui.mcp.Knowledge.PackageVersion.CompareTo(libpjsua2maui.mcp.Knowledge.PackageVersion)">
                <summary>With summary.</summary><returns>Comparison.</returns>
              </member>
              <member name="M:empty.Member">   </member>
              <member name="M:missing.Member"><returns>Unassociated.</returns></member>
            </members></doc>
            """);

        var result = await new PackageKnowledgeExtractor().ExtractAsync(package.Acquisition, TestContext.Current.CancellationToken);

        var parsed = Assert.Single(result.Associations, association => association.Documentation.MemberId.EndsWith("PackageVersion.Parse(System.String)", StringComparison.Ordinal));
        Assert.Contains("Direct member text.", parsed.Documentation.Content);
        Assert.Contains("<param", parsed.Documentation.Content);
        Assert.Contains("<paramref", parsed.Documentation.Content);
        Assert.Contains("<returns>", parsed.Documentation.Content);
        Assert.Contains("<remarks>", parsed.Documentation.Content);
        Assert.Contains("<exception", parsed.Documentation.Content);
        Assert.Contains("<example>", parsed.Documentation.Content);

        var returnsOnly = Assert.Single(result.Documentation, documentation => documentation.MemberId.EndsWith("PackageVersion.ToString", StringComparison.Ordinal));
        Assert.Contains("<returns>", returnsOnly.Content);
        Assert.Contains(result.Documentation, documentation => documentation.MemberId == "M:empty.Member" && string.IsNullOrEmpty(documentation.Content));
        Assert.Contains(result.UnassociatedDocumentation, documentation => documentation.MemberId == "M:missing.Member");
    }

    [Fact]
    public async Task ExtractAsync_ReadsPublicMetadataAndPreservesStructuredXmlPerTfm()
    {
        using var package = CreatePackage("net10.0-android", typeof(PackageVersion).Assembly.Location,
            """
            <doc><members>
              <member name="T:libpjsua2maui.mcp.Knowledge.PackageVersion"><summary>Version.</summary><remarks><see cref="T:System.Version"/> details.</remarks></member>
              <member name="M:libpjsua2maui.mcp.Knowledge.PackageVersion.Parse(System.String)"><summary>Parse.</summary><param name="value">Input.</param><returns>A version.</returns><example>&lt;code&gt;2.17.0.5&lt;/code&gt;</example></member>
              <member name="M:missing.Member"><summary>Not associated.</summary></member>
            </members></doc>
            """);

        var result = await new PackageKnowledgeExtractor().ExtractAsync(package.Acquisition, TestContext.Current.CancellationToken);

        var type = Assert.Single(result.Symbols, symbol => symbol.DocumentationId == "T:libpjsua2maui.mcp.Knowledge.PackageVersion");
        Assert.Equal("net10.0-android", type.TargetFramework);
        Assert.Contains(result.Symbols, symbol => symbol.DocumentationId == "M:libpjsua2maui.mcp.Knowledge.PackageVersion.Parse(System.String)");
        Assert.Equal(3, result.Documentation.Count);
        Assert.Equal(2, result.Associations.Count);
        Assert.Single(result.UnassociatedDocumentation);
        Assert.Contains("<remarks>", result.Associations.Single(a => a.Documentation.MemberId.StartsWith("T:", StringComparison.Ordinal)).Documentation.Content);
        Assert.Contains("<example>", result.Associations.Single(a => a.Documentation.MemberId.StartsWith("M:", StringComparison.Ordinal)).Documentation.Content);
    }

    [Fact]
    public async Task ExtractAsync_UsesAssemblyAndDocumentationTfmIsolation()
    {
        using var package = CreatePackage("net10.0-android", typeof(PackageVersion).Assembly.Location, "<doc><members><member name=\"T:Only.Android\"><summary>Android</summary></member></members></doc>");
        var result = await new PackageKnowledgeExtractor().ExtractAsync(package.Acquisition, TestContext.Current.CancellationToken);

        Assert.All(result.Symbols, symbol => Assert.Equal("net10.0-android", symbol.TargetFramework));
        Assert.All(result.Documentation, item => Assert.Equal("net10.0-android", item.TargetFramework));
        Assert.Equal("2.17.0.5", result.Acquisition.Manifest.PackageVersion.Value);
        Assert.All(result.Documentation, item => Assert.Equal("lib/net10.0-android/libpjsua2maui.xml", item.SourceArtifact.RelativePath));
    }

    [Fact]
    public async Task ExtractAsync_ReportsInvalidAssemblyAndPackageContext()
    {
        using var package = CreatePackage("net10.0-android", null, "<doc />", assemblyContent: "not an assembly");

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => new PackageKnowledgeExtractor().ExtractAsync(package.Acquisition, TestContext.Current.CancellationToken));

        Assert.Contains("libpjsua2maui 2.17.0.5", exception.Message);
        Assert.Contains("net10.0-android", exception.Message);
    }

    [Fact]
    public async Task ExtractAsync_ReportsInvalidXmlInsteadOfReturningEmptyDocumentation()
    {
        using var package = CreatePackage("net10.0-android", typeof(PackageVersion).Assembly.Location, "<doc>");

        var exception = await Assert.ThrowsAsync<InvalidDataException>(() => new PackageKnowledgeExtractor().ExtractAsync(package.Acquisition, TestContext.Current.CancellationToken));

        Assert.Contains("lib/net10.0-android/libpjsua2maui.xml", exception.Message);
    }

    [Fact]
    public async Task ExtractAsync_PreservesValidEmptyDocumentationAndHonorsCancellation()
    {
        using var package = CreatePackage("net10.0-android", typeof(PackageVersion).Assembly.Location, "<doc><members /></doc>");
        var result = await new PackageKnowledgeExtractor().ExtractAsync(package.Acquisition, TestContext.Current.CancellationToken);
        Assert.Empty(result.Documentation);
        Assert.Empty(result.Associations);
        Assert.Empty(result.UnassociatedDocumentation);

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => new PackageKnowledgeExtractor().ExtractAsync(package.Acquisition, cancellation.Token));
    }

    [Theory]
    [InlineData("net10.0-android", "runtimes/android-arm64/native/libpjsua2.so", KnowledgePlatform.Android)]
    [InlineData("net10.0-ios", "runtimes/ios-arm64/native/libpjsua2.a", KnowledgePlatform.iOS)]
    [InlineData("net10.0-maccatalyst", "runtimes/maccatalyst-arm64/native/libpjsua2.a", KnowledgePlatform.MacCatalyst)]
    [InlineData("net10.0-windows", "runtimes/win-x64/native/pjsua2.dll", KnowledgePlatform.Windows)]
    public async Task ExtractAsync_RecordsManagedAndNativePlatformEvidence(string tfm, string nativePath, KnowledgePlatform platform)
    {
        using var package = CreatePackage(tfm, typeof(PackageVersion).Assembly.Location, "<doc><members /></doc>", nativePath: nativePath);

        var result = await new PackageKnowledgeExtractor().ExtractAsync(package.Acquisition, TestContext.Current.CancellationToken);

        var evidence = Assert.Single(result.PlatformEvidence);
        Assert.Equal(platform, evidence.Platform);
        Assert.Equal(tfm, evidence.TargetFramework);
        Assert.Equal(PlatformArtifactStatus.Present, evidence.NativeArtifact);
        Assert.Equal(KnowledgeConfidence.Confirmed, evidence.Confidence);
        Assert.Contains(nativePath, evidence.Provenance!.SourceReference!, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ExtractAsync_LeavesUnknownPlatformEvidenceExplicit()
    {
        using var package = CreatePackage("net10.0", typeof(PackageVersion).Assembly.Location, "<doc><members /></doc>");

        var result = await new PackageKnowledgeExtractor().ExtractAsync(package.Acquisition, TestContext.Current.CancellationToken);

        var evidence = Assert.Single(result.PlatformEvidence);
        Assert.Equal(KnowledgePlatform.Unknown, evidence.Platform);
        Assert.Equal(PlatformArtifactStatus.Unknown, evidence.NativeArtifact);
        Assert.Equal(KnowledgeConfidence.Strong, evidence.Confidence);
    }

    [Fact]
    public async Task ExtractAsync_InspectsPublishedManagedAndNativeSources()
    {
        using var package = CreatePackage(
            "net10.0-windows10.0.19041",
            typeof(PackageVersion).Assembly.Location,
            "<doc><members /></doc>",
            sourceEntries:
            [
                ("src/Platforms/Windows/pjsua2maui/pjsua2/Account.cs", "// Version 4.5.1\npublic class Account { HandleRef p; bool swigCMemOwn; void Dispose() { pjsua2PINVOKE.delete_Account(p); } }"),
                ("src/Platforms/Windows/cpp/pjsua2_wrap.cpp", "#include <pjsua2/endpoint.hpp>\nvoid CSharp_libpjsua2fmaui_Account_delete_Account() {}"),
                ("src/Platforms/Windows/cpp/pjsua2_wrap.hpp", "#include <pjsua2/types.hpp>")
            ]);

        var result = await new PackageKnowledgeExtractor().ExtractAsync(package.Acquisition, TestContext.Current.CancellationToken);

        Assert.Contains(result.Acquisition.Manifest.Artifacts, item => item.Kind == ArtifactKind.ManagedSource);
        Assert.Contains(result.Acquisition.Manifest.Artifacts, item => item.Kind == ArtifactKind.NativeSource);
        Assert.Contains(result.Acquisition.Manifest.Artifacts, item => item.Kind == ArtifactKind.NativeHeader);
        Assert.Contains(result.Relationships, item => item.RelationshipType == "native-to-pjproject-text");
        Assert.Contains(result.LifecycleFacts, item => item.Kind == LifecycleFactKind.Disposal);
    }


    private static TestPackage CreatePackage(string tfm, string? assemblyPath, string xml, string? assemblyContent = null, string? nativePath = null, params (string Path, string Content)[] sourceEntries)
    {
        var path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"libpjsua2maui-extractor-{Guid.NewGuid():N}.nupkg");
        using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create))
        {
            AddEntry(archive, $"lib/{tfm}/libpjsua2maui.dll", assemblyContent is not null ? Encoding.UTF8.GetBytes(assemblyContent) : File.ReadAllBytes(assemblyPath!));
            AddEntry(archive, $"lib/{tfm}/libpjsua2maui.xml", Encoding.UTF8.GetBytes(xml));
            if (nativePath is not null)
            {
                AddEntry(archive, nativePath, []);
            }

            foreach (var source in sourceEntries)
            {
                AddEntry(archive, source.Path, Encoding.UTF8.GetBytes(source.Content));
            }
        }

        var packageIdentity = new PackageIdentity("libpjsua2maui");
        var version = PackageVersion.Parse("2.17.0.5");
        var assembly = new PackageArtifact(packageIdentity, version, $"lib/{tfm}/libpjsua2maui.dll", ArtifactKind.ManagedAssembly, tfm);
        var documentation = new PackageArtifact(packageIdentity, version, $"lib/{tfm}/libpjsua2maui.xml", ArtifactKind.XmlDocumentation, tfm);
        var artifacts = new List<PackageArtifact> { assembly, documentation };
        if (nativePath is not null)
        {
            var runtimeIdentifier = nativePath.Split('/')[1];
            artifacts.Add(new PackageArtifact(packageIdentity, version, nativePath, ArtifactKind.NativeLibrary, runtimeIdentifier: runtimeIdentifier));
        }

        foreach (var source in sourceEntries)
        {
            var kind = Path.GetExtension(source.Path).ToLowerInvariant() switch
            {
                ".cs" => ArtifactKind.ManagedSource,
                ".cpp" => ArtifactKind.NativeSource,
                ".h" or ".hpp" => ArtifactKind.NativeHeader,
                _ => ArtifactKind.Other
            };
            artifacts.Add(new PackageArtifact(packageIdentity, version, source.Path, kind, tfm));
        }
        var manifest = new KnowledgeManifest(new KnowledgeSetIdentity(packageIdentity, version), artifacts: artifacts, status: KnowledgeManifestStatus.Validated);
        var validation = new PackageDocumentationValidation(version, new Dictionary<string, string> { [tfm] = documentation.RelativePath }, [tfm], []);
        return new TestPackage(new PackageAcquisitionResult(path, manifest, validation), path);
    }

    private static void AddEntry(ZipArchive archive, string path, byte[] content)
    {
        using var stream = archive.CreateEntry(path).Open();
        stream.Write(content);
    }

    private sealed class TestPackage(PackageAcquisitionResult acquisition, string path) : IDisposable
    {
        public PackageAcquisitionResult Acquisition { get; } = acquisition;

        public void Dispose()
        {
            File.Delete(path);
        }
    }
}
