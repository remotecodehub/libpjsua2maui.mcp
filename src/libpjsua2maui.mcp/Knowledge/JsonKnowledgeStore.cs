using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace libpjsua2maui.mcp.Knowledge;

/// <summary>Stores canonical knowledge as versioned UTF-8 JSON with safe replacement.</summary>
public sealed class JsonKnowledgeStore : IKnowledgeStore
{
    public const int CurrentSchemaVersion = 1;
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public async Task SaveAsync(string path, IndexedKnowledgeSet knowledge, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(knowledge);
        cancellationToken.ThrowIfCancellationRequested();
        var fullPath = Path.GetFullPath(path);
        var directory = Path.GetDirectoryName(fullPath) ?? throw new InvalidOperationException("The destination has no directory.");
        Directory.CreateDirectory(directory);
        var bytes = JsonSerializer.SerializeToUtf8Bytes(ToDto(knowledge), Options);
        var temporary = Path.Combine(directory, $".{Path.GetFileName(fullPath)}.{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllBytesAsync(temporary, bytes, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            File.Move(temporary, fullPath, overwrite: true);
        }
        catch
        {
            if (File.Exists(temporary)) File.Delete(temporary);
            throw;
        }
    }

    public async Task<IndexedKnowledgeSet> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        await using var stream = new FileStream(Path.GetFullPath(path), FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        KnowledgeDto dto;
        try
        {
            dto = await JsonSerializer.DeserializeAsync<KnowledgeDto>(stream, Options, cancellationToken)
                ?? throw new InvalidDataException("The knowledge artifact is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The knowledge artifact is not valid JSON.", exception);
        }
        if (dto.SchemaVersion != CurrentSchemaVersion)
        {
            throw new InvalidDataException($"Unsupported knowledge schema version {dto.SchemaVersion}; supported version is {CurrentSchemaVersion}.");
        }

        return FromDto(dto);
    }

    private static KnowledgeDto ToDto(IndexedKnowledgeSet value) => new()
    {
        SchemaVersion = CurrentSchemaVersion,
        PackageId = value.Manifest.Package.Id,
        PackageVersion = value.Manifest.PackageVersion.Value,
        KnowledgeRevision = value.Manifest.Identity.Revision,
        ManifestStatus = value.Manifest.Status.ToString(),
        Metadata = value.Manifest.Metadata
            .Where(p => !p.Key.Equals("artifactPath", StringComparison.OrdinalIgnoreCase))
            .OrderBy(p => p.Key, StringComparer.Ordinal)
            .ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal),
        Artifacts = value.Manifest.Artifacts.Select(ArtifactDto.From).ToArray(),
        Symbols = value.Symbols.Select(SymbolDto.From).ToArray(),
        Documentation = value.Documentation.Select(DocumentationDto.From).ToArray(),
        UnassociatedDocumentation = value.UnassociatedDocumentation.Select(DocumentationDto.From).ToArray(),
        Associations = value.Associations.Select(AssociationDto.From).ToArray(),
        Relationships = value.Relationships.Select(RelationshipDto.From).ToArray(),
        LifecycleFacts = value.LifecycleFacts.Select(LifecycleFactDto.From).ToArray(),
        PlatformEvidence = value.PlatformEvidence.Select(PlatformEvidenceDto.From).ToArray()
    };

    private static IndexedKnowledgeSet FromDto(KnowledgeDto dto)
    {
        var packageId = dto.PackageId ?? throw new InvalidDataException("Package ID is required.");
        if (!packageId.Equals("libpjsua2maui", StringComparison.Ordinal))
        {
            throw new InvalidDataException("The persisted package ID is not supported.");
        }

        var package = new PackageIdentity(packageId);
        var version = PackageVersion.Parse(dto.PackageVersion ?? throw new InvalidDataException("Package version is required."));
        var identity = new KnowledgeSetIdentity(package, version, dto.KnowledgeRevision);
        if (!TryParseEnum(dto.ManifestStatus, out KnowledgeManifestStatus status) || status != KnowledgeManifestStatus.Indexed)
            throw new InvalidDataException("The persisted manifest must have Indexed status.");
        var artifacts = (dto.Artifacts ?? throw new InvalidDataException("Artifacts are required.")).Select(a => a.ToModel(package, version, identity)).ToArray();
        var artifactMap = artifacts.ToDictionary(a => a.RelativePath, StringComparer.Ordinal);
        var symbols = (dto.Symbols ?? throw new InvalidDataException("Symbols are required.")).Select(s => s.ToModel(package, version, artifactMap, identity)).ToArray();
        var docs = (dto.Documentation ?? throw new InvalidDataException("Documentation is required")).Select(d => d.ToModel(package, version, artifactMap, identity)).ToArray();
        var unassociated = (dto.UnassociatedDocumentation ?? throw new InvalidDataException("Unassociated documentation is required")).Select(d => d.ToModel(package, version, artifactMap, identity)).ToArray();
        var symbolMap = symbols.ToDictionary(SymbolKey, StringComparer.Ordinal);
        var docMap = docs.ToDictionary(DocumentationKey, StringComparer.Ordinal);
        var associations = (dto.Associations ?? throw new InvalidDataException("Associations are required")).Select(a => a.ToModel(docMap, symbolMap, identity)).ToArray();
        var relationships = (dto.Relationships ?? throw new InvalidDataException("Relationships are required.")).Select(r => r.ToModel(identity, artifactMap, symbolMap)).ToArray();
        var lifecycleFacts = (dto.LifecycleFacts ?? []).Select(f => f.ToModel(identity)).ToArray();
        var platformEvidence = (dto.PlatformEvidence ?? []).Select(f => f.ToModel(identity)).ToArray();
        var manifest = new KnowledgeManifest(identity, dto.Metadata ?? throw new InvalidDataException("Metadata is required."), artifacts, status);
        return new IndexedKnowledgeSet(manifest, symbols, docs, associations, unassociated, relationships, lifecycleFacts, platformEvidence);
    }

    private static bool TryParseEnum<T>(string? value, out T result) where T : struct, Enum
    {
        if (value is not null && !int.TryParse(value, out _) && Enum.TryParse<T>(value, ignoreCase: false, out result) && Enum.IsDefined(result))
        {
            return true;
        }

        result = default;
        return false;
    }

    private static string SymbolKey(SymbolRepresentation s) => $"{s.TargetFramework}|{s.Identity.Kind}|{s.Identity.QualifiedName}|{s.DocumentationId}|{s.Signature}";
    private static string DocumentationKey(DocumentationRecord d) => $"{d.TargetFramework}|{d.MemberId}|{d.SourceArtifact.RelativePath}";

    public sealed class KnowledgeDto
    {
        public int SchemaVersion { get; set; }
        public string? PackageId { get; set; }
        public string? PackageVersion { get; set; }
        public string? KnowledgeRevision { get; set; }
        public string? ManifestStatus { get; set; }
        public Dictionary<string, string>? Metadata { get; set; }
        public ArtifactDto[]? Artifacts { get; set; }
        public SymbolDto[]? Symbols { get; set; }
        public DocumentationDto[]? Documentation { get; set; }
        public DocumentationDto[]? UnassociatedDocumentation { get; set; }
        public AssociationDto[]? Associations { get; set; }
        public RelationshipDto[]? Relationships { get; set; }
        public LifecycleFactDto[]? LifecycleFacts { get; set; }
        public PlatformEvidenceDto[]? PlatformEvidence { get; set; }
    }

    public sealed class ArtifactDto
    {
        public string? Path { get; set; }
        public string? Kind { get; set; }
        public string? TargetFramework { get; set; }
        public string? RuntimeIdentifier { get; set; }
        public ProvenanceDto? Provenance { get; set; }
        public static ArtifactDto From(PackageArtifact x) => new() { Path = x.RelativePath, Kind = x.Kind.ToString(), TargetFramework = x.TargetFramework, RuntimeIdentifier = x.RuntimeIdentifier, Provenance = ProvenanceDto.From(x.Provenance) };
        public PackageArtifact ToModel(PackageIdentity p, PackageVersion v, KnowledgeSetIdentity identity)
        {
            var artifactPath = Path;
            if (!TryParseEnum(Kind, out ArtifactKind kind) || !PackagePathValidator.IsValid(artifactPath)) throw new InvalidDataException("Invalid package artifact.");
            var provenance = Provenance?.ToModel(identity);
            if (provenance?.ArtifactPath is not null && !provenance.ArtifactPath.Equals(artifactPath, StringComparison.Ordinal)) throw new InvalidDataException("Artifact provenance does not reference the artifact.");
            return new PackageArtifact(p, v, artifactPath!, kind, TargetFramework, RuntimeIdentifier, provenance);
        }
    }

    public sealed class SymbolDto
    {
        public string? QualifiedName { get; set; }
        public string? Kind { get; set; }
        public string? TargetFramework { get; set; }
        public string? ArtifactPath { get; set; }
        public string? DocumentationId { get; set; }
        public string? Signature { get; set; }
        public ParameterDto[]? Parameters { get; set; }
        public string? ReturnType { get; set; }
        public ProvenanceDto? Provenance { get; set; }
        public static SymbolDto From(SymbolRepresentation x) => new() { QualifiedName = x.Identity.QualifiedName, Kind = x.Identity.Kind.ToString(), TargetFramework = x.TargetFramework, ArtifactPath = x.SourceArtifact?.RelativePath, DocumentationId = x.DocumentationId, Signature = x.Signature, Parameters = x.Parameters.Select(ParameterDto.From).ToArray(), ReturnType = x.ReturnType, Provenance = ProvenanceDto.From(x.Provenance) };
        public SymbolRepresentation ToModel(PackageIdentity p, PackageVersion v, IReadOnlyDictionary<string, PackageArtifact> artifacts, KnowledgeSetIdentity identity)
        {
            if (!TryParseEnum(Kind, out SymbolKind kind) || QualifiedName is null) throw new InvalidDataException("Invalid symbol identity.");
            PackageArtifact? artifact = null;
            if (ArtifactPath is not null && (!artifacts.TryGetValue(ArtifactPath, out artifact) || artifact.PackageVersion != v || artifact.TargetFramework != TargetFramework)) throw new InvalidDataException("Invalid symbol artifact reference.");
            var provenance = Provenance?.ToModel(identity);
            if (provenance?.ArtifactPath is not null && (artifact is null || !provenance.ArtifactPath.Equals(artifact.RelativePath, StringComparison.Ordinal))) throw new InvalidDataException("Symbol provenance does not reference the symbol artifact.");
            return new SymbolRepresentation(p, v, new SymbolIdentity(QualifiedName, kind), TargetFramework, artifact, DocumentationId, Signature, (Parameters ?? []).Select(x => x.ToModel()).ToArray(), ReturnType, provenance);
        }
    }

    public sealed class ParameterDto
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public bool IsByReference { get; set; }
        public static ParameterDto From(SymbolParameter x) => new() { Name = x.Name, Type = x.Type, IsByReference = x.IsByReference };
        public SymbolParameter ToModel() => new(Name ?? throw new InvalidDataException("Parameter name is required."), Type ?? throw new InvalidDataException("Parameter type is required."), IsByReference);
    }

    public sealed class DocumentationDto
    {
        public string? MemberId { get; set; }
        public string? Content { get; set; }
        public string? ArtifactPath { get; set; }
        public string? TargetFramework { get; set; }
        public ProvenanceDto? Provenance { get; set; }
        public static DocumentationDto From(DocumentationRecord x) => new() { MemberId = x.MemberId, Content = x.Content, ArtifactPath = x.SourceArtifact.RelativePath, TargetFramework = x.TargetFramework, Provenance = ProvenanceDto.From(x.Provenance) };
        public DocumentationRecord ToModel(PackageIdentity p, PackageVersion v, IReadOnlyDictionary<string, PackageArtifact> artifacts, KnowledgeSetIdentity identity)
        {
            if (ArtifactPath is null || !artifacts.TryGetValue(ArtifactPath, out var artifact) || artifact.Kind != ArtifactKind.XmlDocumentation || artifact.TargetFramework != TargetFramework) throw new InvalidDataException("Invalid documentation artifact reference.");
            var provenance = Provenance?.ToModel(identity);
            if (provenance?.ArtifactPath is not null && !provenance.ArtifactPath.Equals(artifact.RelativePath, StringComparison.Ordinal)) throw new InvalidDataException("Documentation provenance does not reference its artifact.");
            return new DocumentationRecord(p, v, MemberId ?? throw new InvalidDataException("Documentation member ID is required."), Content ?? throw new InvalidDataException("Documentation content is required."), artifact, TargetFramework, provenance);
        }
    }

    public sealed class AssociationDto
    {
        public string? DocumentationKey { get; set; }
        public string? SymbolKey { get; set; }
        public ProvenanceDto? Provenance { get; set; }
        public static AssociationDto From(DocumentationAssociation x) => new() { DocumentationKey = JsonKnowledgeStore.DocumentationKey(x.Documentation), SymbolKey = JsonKnowledgeStore.SymbolKey(x.Symbol), Provenance = ProvenanceDto.From(x.Provenance) };
        public DocumentationAssociation ToModel(IReadOnlyDictionary<string, DocumentationRecord> docs, IReadOnlyDictionary<string, SymbolRepresentation> symbols, KnowledgeSetIdentity identity)
        {
            if (DocumentationKey is null || SymbolKey is null || !docs.TryGetValue(DocumentationKey, out var doc) || !symbols.TryGetValue(SymbolKey, out var symbol) || doc.TargetFramework != symbol.TargetFramework) throw new InvalidDataException("Invalid documentation association reference.");
            return new DocumentationAssociation(doc, symbol, Provenance?.ToModel(identity));
        }
    }

    public sealed class RelationshipDto
    {
        public string? Type { get; set; }
        public string? Source { get; set; }
        public string? Target { get; set; }
        public NodeDto? SourceNode { get; set; }
        public NodeDto? TargetNode { get; set; }
        public string? Status { get; set; }
        public string? Confidence { get; set; }
        public ProvenanceDto? Provenance { get; set; }
        public static RelationshipDto From(KnowledgeRelationship x) => new() { Type = x.RelationshipType, Source = x.Source, Target = x.Target, SourceNode = NodeDto.From(x.SourceNode), TargetNode = NodeDto.From(x.TargetNode), Status = x.Status.ToString(), Confidence = x.Confidence.ToString(), Provenance = ProvenanceDto.From(x.Provenance) };
        public KnowledgeRelationship ToModel(KnowledgeSetIdentity identity, IReadOnlyDictionary<string, PackageArtifact> artifacts, IReadOnlyDictionary<string, SymbolRepresentation> symbols)
        {
            var provenance = Provenance?.ToModel(identity);
            if (provenance?.ArtifactPath is not null && !artifacts.ContainsKey(provenance.ArtifactPath)) throw new InvalidDataException("Relationship provenance references an unknown artifact.");
            if (provenance?.Symbol is not null && !symbols.Values.Any(symbol => symbol.Identity == provenance.Symbol)) throw new InvalidDataException("Relationship provenance references an unknown symbol.");
            if (!TryParseEnum(Status ?? KnowledgeFactStatus.Established.ToString(), out KnowledgeFactStatus status) || !TryParseEnum(Confidence ?? KnowledgeConfidence.Unknown.ToString(), out KnowledgeConfidence confidence)) throw new InvalidDataException("Invalid relationship status or confidence.");
            if (SourceNode is null)
            {
                return new KnowledgeRelationship(identity, Type ?? throw new InvalidDataException("Relationship type is required."), Source ?? throw new InvalidDataException("Relationship source is required."), Target, provenance, status, confidence);
            }

            return new KnowledgeRelationship(identity, Type ?? throw new InvalidDataException("Relationship type is required."), SourceNode.ToModel(), TargetNode?.ToModel(), status, provenance, confidence);
        }
    }

    public sealed class NodeDto
    {
        public string? Kind { get; set; }
        public string? Resolution { get; set; }
        public string? Identifier { get; set; }
        public static NodeDto From(KnowledgeNodeReference x) => new() { Kind = x.Kind.ToString(), Resolution = x.Resolution.ToString(), Identifier = x.Identifier };
        public KnowledgeNodeReference ToModel()
        {
            if (!TryParseEnum(Kind, out KnowledgeNodeKind kind) || !TryParseEnum(Resolution, out KnowledgeNodeResolution resolution)) throw new InvalidDataException("Invalid knowledge node reference.");
            return new KnowledgeNodeReference(kind, resolution, Identifier);
        }
    }

    public sealed class LifecycleFactDto
    {
        public NodeDto? Subject { get; set; }
        public string? Kind { get; set; }
        public string? Status { get; set; }
        public string? Confidence { get; set; }
        public string? Value { get; set; }
        public ProvenanceDto? Provenance { get; set; }
        public static LifecycleFactDto From(LifecycleFact x) => new() { Subject = NodeDto.From(x.Subject), Kind = x.Kind.ToString(), Status = x.Status.ToString(), Confidence = x.Confidence.ToString(), Value = x.Value, Provenance = ProvenanceDto.From(x.Provenance) };
        public LifecycleFact ToModel(KnowledgeSetIdentity identity)
        {
            if (Subject is null || !TryParseEnum(Kind, out LifecycleFactKind kind) || !TryParseEnum(Status, out KnowledgeFactStatus status) || !TryParseEnum(Confidence ?? KnowledgeConfidence.Unknown.ToString(), out KnowledgeConfidence confidence)) throw new InvalidDataException("Invalid lifecycle fact.");
            return new LifecycleFact(identity, Subject.ToModel(), kind, status, Value, Provenance?.ToModel(identity), confidence);
        }
    }

    public sealed class PlatformEvidenceDto
    {
        public NodeDto? Subject { get; set; }
        public string? Platform { get; set; }
        public string? TargetFramework { get; set; }
        public string? RuntimeIdentifier { get; set; }
        public string? ManagedAssembly { get; set; }
        public string? NativeArtifact { get; set; }
        public string? ManagedSource { get; set; }
        public string? NativeSource { get; set; }
        public string? NativeHeader { get; set; }
        public string? Runtime { get; set; }
        public string[]? ArtifactPaths { get; set; }
        public string? Confidence { get; set; }
        public string? Limitations { get; set; }
        public ProvenanceDto? Provenance { get; set; }
        public static PlatformEvidenceDto From(PlatformEvidence x) => new()
        {
            Subject = NodeDto.From(x.Subject), Platform = x.Platform.ToString(), TargetFramework = x.TargetFramework,
            RuntimeIdentifier = x.RuntimeIdentifier, ManagedAssembly = x.ManagedAssembly.ToString(), NativeArtifact = x.NativeArtifact.ToString(),
            ManagedSource = x.ManagedSource.ToString(), NativeSource = x.NativeSource.ToString(), NativeHeader = x.NativeHeader.ToString(), Runtime = x.Runtime.ToString(), ArtifactPaths = x.ArtifactPaths.ToArray(),
            Confidence = x.Confidence.ToString(), Limitations = x.Limitations, Provenance = ProvenanceDto.From(x.Provenance)
        };
        public PlatformEvidence ToModel(KnowledgeSetIdentity identity)
        {
            if (Subject is null || !TryParseEnum(Platform, out KnowledgePlatform platform) || !TryParseEnum(ManagedAssembly, out PlatformArtifactStatus managed) || !TryParseEnum(NativeArtifact, out PlatformArtifactStatus native) || !TryParseEnum(Confidence, out KnowledgeConfidence confidence)) throw new InvalidDataException("Invalid platform evidence.");
            if (!TryParseEnum(ManagedSource ?? PlatformArtifactStatus.Unknown.ToString(), out PlatformArtifactStatus managedSource) || !TryParseEnum(NativeSource ?? PlatformArtifactStatus.Unknown.ToString(), out PlatformArtifactStatus nativeSource) || !TryParseEnum(NativeHeader ?? PlatformArtifactStatus.Unknown.ToString(), out PlatformArtifactStatus nativeHeader) || !TryParseEnum(Runtime ?? PlatformRuntimeStatus.NotValidated.ToString(), out PlatformRuntimeStatus runtime)) throw new InvalidDataException("Invalid platform source or runtime evidence.");
            return new PlatformEvidence(identity, Subject.ToModel(), platform, TargetFramework, RuntimeIdentifier, managed, native, confidence, Provenance?.ToModel(identity), Limitations, managedSource, nativeSource, nativeHeader, runtime, ArtifactPaths);
        }
    }

    public sealed class ProvenanceDto
    {
        public string? Kind { get; set; }
        public string? ArtifactPath { get; set; }
        public string? SymbolQualifiedName { get; set; }
        public string? SymbolKind { get; set; }
        public string? SourceReference { get; set; }
        public string? Limitations { get; set; }
        public string? Repository { get; set; }
        public string? Revision { get; set; }
        public string? Tag { get; set; }
        public string? TargetFramework { get; set; }
        public string? RuntimeIdentifier { get; set; }
        public string? Evidence { get; set; }
        public static ProvenanceDto? From(FactProvenance? x) => x is null ? null : new()
        {
            Kind = x.Kind.ToString(), ArtifactPath = x.ArtifactPath, SymbolQualifiedName = x.Symbol?.QualifiedName,
            SymbolKind = x.Symbol?.Kind.ToString(), SourceReference = x.SourceReference, Limitations = x.Limitations,
            Repository = x.Repository, Revision = x.Revision, Tag = x.Tag, TargetFramework = x.TargetFramework,
            RuntimeIdentifier = x.RuntimeIdentifier, Evidence = x.Evidence
        };
        public FactProvenance ToModel(KnowledgeSetIdentity identity)
        {
            if (!TryParseEnum(Kind, out ProvenanceKind kind)) throw new InvalidDataException("Invalid provenance kind.");
            SymbolIdentity? symbol = null;
            if (SymbolQualifiedName is not null)
            {
                if (!TryParseEnum(SymbolKind, out SymbolKind symbolKind)) throw new InvalidDataException("Invalid provenance symbol kind.");
                symbol = new SymbolIdentity(SymbolQualifiedName, symbolKind);
            }
            return new FactProvenance(identity, kind, ArtifactPath, symbol, SourceReference, Limitations, Repository, Revision, Tag, TargetFramework, RuntimeIdentifier, Evidence);
        }
    }
}
