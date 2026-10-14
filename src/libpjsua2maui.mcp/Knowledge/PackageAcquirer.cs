using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;

namespace libpjsua2maui.mcp.Knowledge;

/// <summary>Downloads NuGet flat-container packages and validates their package-specific knowledge prerequisites.</summary>
public sealed class PackageAcquirer(HttpClient httpClient, PackageAcquisitionOptions options) : IPackageAcquirer
{
    private const string PackageId = "libpjsua2maui";
    private readonly HttpClient _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    private readonly PackageAcquisitionOptions _options = options ?? throw new ArgumentNullException(nameof(options));

    public async Task<PackageAcquisitionResult> AcquireAsync(string version, CancellationToken cancellationToken = default)
    {
        var packageVersion = PackageVersion.Parse(version);
        var destinationDirectory = Path.Combine(_options.StorageDirectory, PackageId, packageVersion.Value);
        var artifactPath = Path.Combine(destinationDirectory, $"{PackageId}.{packageVersion.Value}.nupkg");

        if (File.Exists(artifactPath))
        {
            return await ValidateStoredArtifactAsync(artifactPath, packageVersion, cancellationToken);
        }

        Directory.CreateDirectory(destinationDirectory);
        var temporaryPath = Path.Combine(destinationDirectory, $".{Path.GetFileName(artifactPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            using var response = await _httpClient.GetAsync(BuildPackageUri(packageVersion), HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"NuGet package acquisition failed with HTTP {(int)response.StatusCode} ({response.ReasonPhrase}).");
            }

            await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
            await using (var destination = new FileStream(temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                await source.CopyToAsync(destination, cancellationToken);
            }

            var validated = await ValidateStoredArtifactAsync(temporaryPath, packageVersion, cancellationToken);
            File.Move(temporaryPath, artifactPath);
            return new PackageAcquisitionResult(artifactPath, validated.Manifest, validated.Documentation);
        }
        catch
        {
            DeleteIfPresent(temporaryPath);
            throw;
        }
    }

    private Uri BuildPackageUri(PackageVersion version)
    {
        var baseUri = _options.PackageSource.AbsoluteUri.TrimEnd('/') + "/";
        var normalizedVersion = version.Value.ToLowerInvariant();
        return new Uri($"{baseUri}{PackageId}/{normalizedVersion}/{PackageId}.{normalizedVersion}.nupkg", UriKind.Absolute);
    }

    private static async Task<PackageAcquisitionResult> ValidateStoredArtifactAsync(string artifactPath, PackageVersion packageVersion, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            await using var stream = new FileStream(artifactPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
            var entries = archive.Entries
                .Where(entry => !string.IsNullOrEmpty(entry.Name))
                .ToDictionary(entry => entry.FullName.Replace('\\', '/'), StringComparer.OrdinalIgnoreCase);
            var packageEntry = entries.FirstOrDefault(pair => pair.Key.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase));
            if (packageEntry.Key is null)
            {
                throw new InvalidDataException("The package does not contain a .nuspec manifest.");
            }

            ValidatePackageManifest(packageEntry.Value, packageVersion);

            var assemblies = entries.Keys
                .Where(path => path.StartsWith("lib/", StringComparison.OrdinalIgnoreCase) && path.EndsWith($"/{PackageId}.dll", StringComparison.OrdinalIgnoreCase))
                .Select(path => path.Split('/'))
                .Where(parts => parts.Length == 3 && !string.IsNullOrWhiteSpace(parts[1]))
                .ToDictionary(parts => parts[1], parts => string.Join('/', parts), StringComparer.Ordinal);
            if (assemblies.Count == 0)
            {
                throw new InvalidDataException($"The package does not contain a lib/<TFM>/{PackageId}.dll assembly.");
            }

            var expectedXml = assemblies.ToDictionary(pair => pair.Key, pair => $"lib/{pair.Key}/{PackageId}.xml", StringComparer.Ordinal);
            var documented = expectedXml.Where(pair => entries.ContainsKey(pair.Value)).Select(pair => pair.Key).ToArray();
            var missing = expectedXml.Keys.Except(documented, StringComparer.Ordinal).ToArray();
            var documentation = new PackageDocumentationValidation(packageVersion, expectedXml, documented, missing);
            if (!documentation.IsValid)
            {
                throw new InvalidDataException($"The package is missing XML documentation for TFM(s): {string.Join(", ", missing)}.");
            }

            var package = new PackageIdentity(PackageId);
            var artifacts = new List<PackageArtifact> { new(package, packageVersion, packageEntry.Key, ArtifactKind.PackageMetadata) };
            foreach (var pair in assemblies)
            {
                artifacts.Add(new PackageArtifact(package, packageVersion, pair.Value, ArtifactKind.ManagedAssembly, pair.Key));
                artifacts.Add(new PackageArtifact(package, packageVersion, expectedXml[pair.Key], ArtifactKind.XmlDocumentation, pair.Key));
            }

            var manifest = new KnowledgeManifest(
                new KnowledgeSetIdentity(package, packageVersion),
                new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["artifactPath"] = Path.GetFullPath(artifactPath),
                    ["source"] = "NuGet flat container"
                },
                artifacts,
                KnowledgeManifestStatus.Validated);
            return new PackageAcquisitionResult(artifactPath, manifest, documentation);
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            throw new InvalidDataException("The acquired file is not a readable NuGet package.", exception);
        }
    }

    private static void ValidatePackageManifest(ZipArchiveEntry entry, PackageVersion expectedVersion)
    {
        try
        {
            using var manifestStream = entry.Open();
            var document = XDocument.Load(manifestStream);
            var metadata = document.Descendants().FirstOrDefault(element => element.Name.LocalName == "metadata");
            var id = metadata?.Elements().FirstOrDefault(element => element.Name.LocalName == "id")?.Value;
            var version = metadata?.Elements().FirstOrDefault(element => element.Name.LocalName == "version")?.Value;
            if (!string.Equals(id, PackageId, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(version, expectedVersion.Value, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("The package manifest identity or version does not match the requested package.");
            }
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception) when (exception is XmlException or InvalidOperationException)
        {
            throw new InvalidDataException("The package manifest is not valid NuGet XML.", exception);
        }
    }

    private static void DeleteIfPresent(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
