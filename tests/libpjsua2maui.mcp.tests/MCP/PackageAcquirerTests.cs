using System.Net;
using System.Net.Http.Headers;
using System.IO.Compression;
using System.Text;
using libpjsua2maui.mcp.Knowledge;

namespace libpjsua2maui.mcp.tests.MCP;

public sealed class PackageAcquirerTests
{
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
