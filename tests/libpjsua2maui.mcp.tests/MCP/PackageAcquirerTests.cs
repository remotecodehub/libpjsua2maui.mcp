using System.Net;
using System.Net.Http.Headers;
using System.IO.Compression;
using System.Text;
using libpjsua2maui.mcp.Knowledge;

namespace libpjsua2maui.mcp.tests.MCP;

public sealed class PackageAcquirerTests
{
    [Fact]
    public void Constructor_RejectsMissingDependencies()
    {
        using var directory = new TemporaryDirectory();
        var options = new PackageAcquisitionOptions(directory.Path);

        Assert.Throws<ArgumentNullException>(() => new PackageAcquirer(null!, options));
        Assert.Throws<ArgumentNullException>(() => new PackageAcquirer(new HttpClient(), null!));
    }

    [Fact]
    public async Task AcquireAsync_DownloadsValidPackageAndReportsDocumentationPerTfm()
    {
        using var directory = new TemporaryDirectory();
        var handler = new StubHandler(CreatePackage(("net10.0-android", true), ("net10.0-ios", true)));
        var acquirer = CreateAcquirer(directory.Path, handler);

        var result = await acquirer.AcquireAsync("2.17.0.4", TestContext.Current.CancellationToken);

        Assert.Equal("2.17.0.4", result.Manifest.PackageVersion.Value);
        Assert.True(File.Exists(result.ArtifactPath));
        Assert.Equal(["net10.0-android", "net10.0-ios"], result.Documentation.DocumentedTfms);
        Assert.Empty(result.Documentation.MissingDocumentationTfms);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task AcquireAsync_ReusesExistingValidatedArtifactWithoutNetwork()
    {
        using var directory = new TemporaryDirectory();
        var handler = new StubHandler(CreatePackage(("net10.0-android", true)));
        var acquirer = CreateAcquirer(directory.Path, handler);

        var first = await acquirer.AcquireAsync("2.17.0.4", TestContext.Current.CancellationToken);
        var second = await acquirer.AcquireAsync("2.17.0.4", TestContext.Current.CancellationToken);

        Assert.Equal(first.ArtifactPath, second.ArtifactPath);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task AcquireAsync_RejectsMissingDocumentationAndLeavesNoPackageArtifact()
    {
        using var directory = new TemporaryDirectory();
        var handler = new StubHandler(CreatePackage(("net10.0-android", false)));
        var acquirer = CreateAcquirer(directory.Path, handler);

        await Assert.ThrowsAsync<InvalidDataException>(() => acquirer.AcquireAsync("2.17.0.4", TestContext.Current.CancellationToken));

        Assert.Empty(Directory.EnumerateFiles(directory.Path, "*.nupkg", SearchOption.AllDirectories));
        Assert.Empty(Directory.EnumerateFiles(directory.Path, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task AcquireAsync_RejectsCorruptResponse()
    {
        using var directory = new TemporaryDirectory();
        var acquirer = CreateAcquirer(directory.Path, new StubHandler(Encoding.UTF8.GetBytes("not a zip")));

        await Assert.ThrowsAsync<InvalidDataException>(() => acquirer.AcquireAsync("2.17.0.4", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AcquireAsync_RejectsMalformedXmlButAcceptsDocumentationWithoutSummary()
    {
        using var directory = new TemporaryDirectory();
        var handler = new StubHandler(CreatePackageWithXml("<doc><members><member name=\"M:Example.Call\"><returns>Result</returns></member></members></doc>"));
        var result = await CreateAcquirer(directory.Path, handler).AcquireAsync("2.17.0.4", TestContext.Current.CancellationToken);
        Assert.True(result.Documentation.IsValid);

        using var malformedDirectory = new TemporaryDirectory();
        var malformed = new StubHandler(CreatePackageWithXml("<doc><members>"));
        await Assert.ThrowsAsync<InvalidDataException>(() => CreateAcquirer(malformedDirectory.Path, malformed).AcquireAsync("2.17.0.4", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AcquireAsync_DiscoversDeterministicAssetsWithTfmAndRidMetadata()
    {
        using var directory = new TemporaryDirectory();
        var handler = new StubHandler(CreateAssetPackage());
        var result = await CreateAcquirer(directory.Path, handler).AcquireAsync("2.17.0.4", TestContext.Current.CancellationToken);

        var artifacts = result.Manifest.Artifacts.OrderBy(artifact => artifact.RelativePath, StringComparer.Ordinal).ToArray();
        Assert.Equal(artifacts.Select(artifact => artifact.RelativePath).Distinct(StringComparer.OrdinalIgnoreCase).Count(), artifacts.Length);
        Assert.Contains(artifacts, artifact => artifact.RelativePath == "lib/net10.0-android/libpjsua2maui.dll" && artifact.Kind == ArtifactKind.ManagedAssembly && artifact.TargetFramework == "net10.0-android" && artifact.RuntimeIdentifier is null);
        Assert.Contains(artifacts, artifact => artifact.RelativePath == "ref/net10.0/libpjsua2maui.dll" && artifact.Kind == ArtifactKind.ReferenceAssembly && artifact.TargetFramework == "net10.0" && artifact.RuntimeIdentifier is null);
        Assert.Contains(artifacts, artifact => artifact.RelativePath == "runtimes/android-arm64/lib/net10.0-android/libpjsua2maui.dll" && artifact.Kind == ArtifactKind.ManagedAssembly && artifact.TargetFramework == "net10.0-android" && artifact.RuntimeIdentifier == "android-arm64");
        Assert.Contains(artifacts, artifact => artifact.RelativePath == "runtimes/android-arm64/native/libpjsua2maui.so" && artifact.Kind == ArtifactKind.NativeLibrary && artifact.TargetFramework is null && artifact.RuntimeIdentifier == "android-arm64");
        Assert.Contains(artifacts, artifact => artifact.RelativePath == "build/swig/libpjsua2maui.i" && artifact.Kind == ArtifactKind.SwigArtifact);
        Assert.DoesNotContain(artifacts, artifact => artifact.Kind == ArtifactKind.NativeLibrary && artifact.RelativePath.Contains("net10.0-android", StringComparison.Ordinal));
        Assert.Contains(artifacts, artifact => artifact.RelativePath == "runtimes/android-arm64/native/libpjsua2maui.dylib" && artifact.Kind == ArtifactKind.NativeLibrary);
        Assert.Contains(artifacts, artifact => artifact.RelativePath == "runtimes/android-arm64/native/readme.txt" && artifact.Kind == ArtifactKind.Other);
        Assert.Contains(artifacts, artifact => artifact.RelativePath == "buildTransitive/libpjsua2maui.props" && artifact.Kind == ArtifactKind.PackageMetadata);
        Assert.Contains(artifacts, artifact => artifact.RelativePath == "build/pinvoke/generated.cs" && artifact.Kind == ArtifactKind.SwigArtifact);
        Assert.Contains(artifacts, artifact => artifact.RelativePath == "tools/Example.SWIG.data" && artifact.Kind == ArtifactKind.SwigArtifact);
    }

    [Theory]
    [InlineData("2.17.0.3")]
    [InlineData("not-a-version")]
    public async Task AcquireAsync_RejectsUnsupportedOrMalformedVersion(string version)
    {
        using var directory = new TemporaryDirectory();
        var handler = new StubHandler(CreatePackage(("net10.0-android", true)));
        var acquirer = CreateAcquirer(directory.Path, handler);

        await Assert.ThrowsAnyAsync<Exception>(() => acquirer.AcquireAsync(version, TestContext.Current.CancellationToken));
        Assert.Equal(0, handler.RequestCount);
    }

    [Fact]
    public async Task AcquireAsync_RejectsHttpFailureWithoutLeavingTemporaryFiles()
    {
        using var directory = new TemporaryDirectory();
        var handler = new StatusHandler(HttpStatusCode.NotFound);

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateAcquirer(directory.Path, handler).AcquireAsync("2.17.0.4", TestContext.Current.CancellationToken));

        Assert.Empty(Directory.EnumerateFiles(directory.Path, "*.tmp", SearchOption.AllDirectories));
    }

    [Fact]
    public async Task AcquireAsync_HonorsCancellationBeforeNetworkAccess()
    {
        using var directory = new TemporaryDirectory();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var handler = new StubHandler(CreatePackage(("net10.0-android", true)));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateAcquirer(directory.Path, handler).AcquireAsync("2.17.0.4", cancellation.Token));
        Assert.Equal(1, handler.RequestCount);
    }

    [Theory]
    [InlineData("other", "2.17.0.4", "identity")]
    [InlineData("libpjsua2maui", "2.17.0.5", "identity")]
    [InlineData("libpjsua2maui", "2.17.0.4", "malformed")]
    public async Task AcquireAsync_RejectsManifestIdentityAndXmlVariants(string id, string version, string shape)
    {
        using var directory = new TemporaryDirectory();
        var manifest = shape == "malformed"
            ? "<package><metadata>"
            : $"<package><metadata><id>{id}</id><version>{version}</version></metadata></package>";
        var handler = new StubHandler(CreateCustomPackage(manifest, ("lib/net10.0-android/libpjsua2maui.dll", "managed"), ("lib/net10.0-android/libpjsua2maui.xml", "<doc />")));

        await Assert.ThrowsAsync<InvalidDataException>(() => CreateAcquirer(directory.Path, handler).AcquireAsync("2.17.0.4", TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task AcquireAsync_RejectsUnsafeArchiveEntryPath()
    {
        using var directory = new TemporaryDirectory();
        var content = CreateCustomPackage(
            "<package><metadata><id>libpjsua2maui</id><version>2.17.0.4</version></metadata></package>",
            ("../bad.txt", "bad"),
            ("lib/net10.0-android/libpjsua2maui.dll", "managed"),
            ("lib/net10.0-android/libpjsua2maui.xml", "<doc />"));

        await Assert.ThrowsAsync<InvalidDataException>(() => CreateAcquirer(directory.Path, new StubHandler(content)).AcquireAsync("2.17.0.4", TestContext.Current.CancellationToken));
    }

    private static PackageAcquirer CreateAcquirer(string directory, HttpMessageHandler handler)
    {
        return new PackageAcquirer(
            new HttpClient(handler),
            new PackageAcquisitionOptions(directory, new Uri("https://example.test/v3-flatcontainer/")));
    }

    private static byte[] CreatePackage(params (string Tfm, bool HasXml)[] tfms)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(archive, "libpjsua2maui.nuspec", "<package><metadata><id>libpjsua2maui</id><version>2.17.0.4</version></metadata></package>");
            foreach (var (tfm, hasXml) in tfms)
            {
                AddEntry(archive, $"lib/{tfm}/libpjsua2maui.dll", "managed");
                if (hasXml)
                {
                    AddEntry(archive, $"lib/{tfm}/libpjsua2maui.xml", "<doc />");
                }
            }
        }

        return stream.ToArray();
    }

    private static byte[] CreateCustomPackage(string manifest, params (string Path, string Content)[] entries)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(archive, "libpjsua2maui.nuspec", manifest);
            foreach (var entry in entries)
            {
                AddEntry(archive, entry.Path, entry.Content);
            }
        }

        return stream.ToArray();
    }

    private static byte[] CreateAssetPackage()
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(archive, "libpjsua2maui.nuspec", "<package><metadata><id>libpjsua2maui</id><version>2.17.0.4</version></metadata></package>");
            AddEntry(archive, "lib/net10.0-android/libpjsua2maui.dll", "managed");
            AddEntry(archive, "lib/net10.0-android/libpjsua2maui.xml", "<doc />");
            AddEntry(archive, "ref/net10.0/libpjsua2maui.dll", "reference");
            AddEntry(archive, "runtimes/android-arm64/lib/net10.0-android/libpjsua2maui.dll", "managed");
            AddEntry(archive, "runtimes/android-arm64/native/libpjsua2maui.so", "native");
            AddEntry(archive, "runtimes/android-arm64/native/libpjsua2maui.dll", "native");
            AddEntry(archive, "runtimes/android-arm64/native/libpjsua2maui.dylib", "native");
            AddEntry(archive, "runtimes/android-arm64/native/libpjsua2maui.a", "native");
            AddEntry(archive, "runtimes/android-arm64/native/libpjsua2maui.lib", "native");
            AddEntry(archive, "runtimes/android-arm64/native/libpjsua2maui.framework", "native");
            AddEntry(archive, "runtimes/android-arm64/native/readme.txt", "other");
            AddEntry(archive, "runtimes/android-arm64/lib/net10.0-android/readme.txt", "other");
            AddEntry(archive, "ref/net10.0/readme.txt", "other");
            AddEntry(archive, "build/swig/libpjsua2maui.i", "%module example");
            AddEntry(archive, "build/native/example-swig-wrapper.h", "header");
            AddEntry(archive, "build/pinvoke/generated.cs", "pinvoke");
            AddEntry(archive, "buildTransitive/libpjsua2maui.props", "props");
            AddEntry(archive, "build/libpjsua2maui.targets", "targets");
            AddEntry(archive, "tools/Example.SWIG.data", "swig");
            AddEntry(archive, "docs/readme.targets", "targets");
            AddEntry(archive, "README.md", "package notes");
        }


        return stream.ToArray();
    }

    private static byte[] CreatePackageWithXml(string xml)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddEntry(archive, "libpjsua2maui.nuspec", "<package><metadata><id>libpjsua2maui</id><version>2.17.0.4</version></metadata></package>");
            AddEntry(archive, "lib/net10.0-android/libpjsua2maui.dll", "managed");
            AddEntry(archive, "lib/net10.0-android/libpjsua2maui.xml", xml);
        }

        return stream.ToArray();
    }

    private static void AddEntry(ZipArchive archive, string path, string content)
    {
        using var writer = new StreamWriter(archive.CreateEntry(path).Open(), Encoding.UTF8);
        writer.Write(content);
    }

    private sealed class StubHandler(byte[] content) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(content)
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            return Task.FromResult(response);
        }
    }

    private sealed class StatusHandler(HttpStatusCode status) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(status) { ReasonPhrase = "fixture failure" });
        }
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory() => Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"libpjsua2maui-tests-{Guid.NewGuid():N}");

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
