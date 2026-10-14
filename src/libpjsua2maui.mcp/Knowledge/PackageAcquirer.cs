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
                .Select(entry => (Path: NormalizeEntryPath(entry.FullName), Entry: entry))
                .ToDictionary(pair => pair.Path, pair => pair.Entry, StringComparer.OrdinalIgnoreCase);
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
            foreach (var pair in expectedXml)
            {
                ValidateXmlDocumentation(entries[pair.Value], pair.Key, pair.Value);
            }

            var artifacts = DiscoverArtifacts(entries, package, packageVersion);

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
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or ArgumentException)
        {
            throw new InvalidDataException("The acquired file is not a readable NuGet package.", exception);
        }
    }

    private static IReadOnlyList<PackageArtifact> DiscoverArtifacts(
        IReadOnlyDictionary<string, ZipArchiveEntry> entries,
        PackageIdentity package,
        PackageVersion packageVersion)
    {
        var artifacts = new List<PackageArtifact>(entries.Count);
        foreach (var path in entries.Keys.Order(StringComparer.OrdinalIgnoreCase))
        {
            var parts = path.Split('/');
            var kind = ArtifactKind.Other;
            string? tfm = null;
            string? rid = null;

            if (path.Equals("libpjsua2maui.nuspec", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".nuspec", StringComparison.OrdinalIgnoreCase))
            {
                kind = ArtifactKind.PackageMetadata;
            }
            else if (parts.Length >= 3 && parts[0].Equals("lib", StringComparison.OrdinalIgnoreCase))
            {
                tfm = parts[1];
                kind = ClassifyManagedOrDocumentation(path);
            }
            else if (parts.Length >= 3 && parts[0].Equals("ref", StringComparison.OrdinalIgnoreCase))
            {
                tfm = parts[1];
                kind = path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                    ? ArtifactKind.ReferenceAssembly
                    : ArtifactKind.Other;
            }
            else if (parts.Length >= 4 && parts[0].Equals("runtimes", StringComparison.OrdinalIgnoreCase))
            {
                rid = parts[1];
                if (parts[2].Equals("lib", StringComparison.OrdinalIgnoreCase))
                {
                    tfm = parts[3];
                    kind = path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
                        ? ArtifactKind.ManagedAssembly
                        : ArtifactKind.Other;
                }
                else if (parts[2].Equals("native", StringComparison.OrdinalIgnoreCase))
                {
                    kind = IsNativeLibraryPath(path) ? ArtifactKind.NativeLibrary : ArtifactKind.Other;
                }
            }

            if (kind == ArtifactKind.Other && IsSwigPath(path))
            {
                kind = ArtifactKind.SwigArtifact;
            }
            else if (kind == ArtifactKind.Other && IsPackageMetadataPath(path))
            {
                kind = ArtifactKind.PackageMetadata;
            }

            artifacts.Add(new PackageArtifact(package, packageVersion, path, kind, tfm, rid));
        }

        return artifacts;
    }

    private static ArtifactKind ClassifyManagedOrDocumentation(string path)
    {
        if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            return ArtifactKind.ManagedAssembly;
        }

        return path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) &&
            Path.GetFileNameWithoutExtension(path).Equals(PackageId, StringComparison.OrdinalIgnoreCase)
            ? ArtifactKind.XmlDocumentation
            : ArtifactKind.Other;
    }

    private static bool IsNativeLibraryPath(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() is ".dll" or ".so" or ".dylib" or ".a" or ".lib" or ".framework";
    }

    private static bool IsSwigPath(string path)
    {
        var fileName = Path.GetFileName(path);
        return fileName.Contains("swig", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".i", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("pinvoke", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPackageMetadataPath(string path)
    {
        return path.StartsWith("build/", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWith("buildTransitive/", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".props", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".targets", StringComparison.OrdinalIgnoreCase);
    }

    private static void ValidateXmlDocumentation(ZipArchiveEntry entry, string tfm, string path)
    {
        try
        {
            using var stream = entry.Open();
            using var reader = XmlReader.Create(stream, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit });
            while (reader.Read())
            {
            }
        }
        catch (XmlException exception)
        {
            throw new InvalidDataException($"The XML documentation at '{path}' for TFM '{tfm}' is not valid XML.", exception);
        }
    }

    private static string NormalizeEntryPath(string path)
    {
        var normalized = path.Replace('\\', '/');
        var segments = normalized.Split('/');
        if (string.IsNullOrWhiteSpace(normalized) || normalized.StartsWith('/') || Path.IsPathRooted(normalized) ||
            segments.Any(segment => segment is "" or "." or ".."))
        {
            throw new InvalidDataException($"The package contains an invalid relative entry path: '{path}'.");
        }

        return normalized;
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
