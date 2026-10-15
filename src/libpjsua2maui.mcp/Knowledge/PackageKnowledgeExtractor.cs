using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Xml;
using System.Xml.Linq;

namespace libpjsua2maui.mcp.Knowledge;

/// <summary>Reads package assemblies as PE metadata and parses per-TFM XML documentation.</summary>
public sealed class PackageKnowledgeExtractor : IPackageKnowledgeExtractor
{
    private readonly SourceRepositoryDescriptor? _sourceRepository;

    public PackageKnowledgeExtractor(SourceRepositoryDescriptor? sourceRepository = null)
    {
        _sourceRepository = sourceRepository;
    }

    public async Task<PackageExtractionResult> ExtractAsync(PackageAcquisitionResult acquisition, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(acquisition);
        var symbols = new List<SymbolRepresentation>();
        var documentation = new List<DocumentationRecord>();
        var platformEvidence = new List<PlatformEvidence>();
        var relationships = new List<KnowledgeRelationship>();
        var lifecycleFacts = new List<LifecycleFact>();
        var sourceInspections = new List<SourceInspectionResult>();
        var sourceCoverage = new SourceInspectionCoverage();
        var sourceDiagnostics = new List<SourceInspectionDiagnostic>();
        var sourceContents = new Dictionary<string, string>(StringComparer.Ordinal);
        var primaryAssemblies = acquisition.Manifest.Artifacts
            .Where(a => a.Kind == ArtifactKind.ManagedAssembly && IsPrimaryLibAssembly(a))
            .OrderBy(a => a.TargetFramework, StringComparer.Ordinal)
            .ToArray();
        var inspectedAssemblies = 0;

        await using var packageStream = new FileStream(acquisition.ArtifactPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        using var archive = new System.IO.Compression.ZipArchive(packageStream, System.IO.Compression.ZipArchiveMode.Read);
        var entries = archive.Entries.ToDictionary(entry => entry.FullName.Replace('\\', '/'), StringComparer.OrdinalIgnoreCase);

        var sourceInspector = new SourceInspection();
        foreach (var sourceArtifact in acquisition.Manifest.Artifacts
            .Where(a => a.Kind is ArtifactKind.ManagedSource or ArtifactKind.NativeSource or ArtifactKind.NativeHeader)
            .OrderBy(a => a.RelativePath, StringComparer.Ordinal))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!entries.TryGetValue(sourceArtifact.RelativePath, out var sourceEntry))
            {
                continue;
            }

            try
            {
                using var reader = new StreamReader(sourceEntry.Open());
                var content = await reader.ReadToEndAsync(cancellationToken);
                sourceContents[sourceArtifact.RelativePath] = content;
                var inspection = sourceInspector.Inspect(acquisition, sourceArtifact, content);
                sourceInspections.Add(inspection);
                sourceCoverage = sourceCoverage.Add(inspection.Coverage ?? new SourceInspectionCoverage());
                sourceDiagnostics.AddRange(inspection.Diagnostics ?? []);
                relationships.AddRange(inspection.Relationships);
                lifecycleFacts.AddRange(inspection.LifecycleFacts);
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException)
            {
                throw new InvalidDataException($"Unable to inspect source artifact '{sourceArtifact.RelativePath}'.", exception);
            }
        }

        foreach (var assemblyArtifact in primaryAssemblies)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var assemblyEntry = entries[assemblyArtifact.RelativePath];
            try
            {
                await using var input = assemblyEntry.Open();
                using var memory = new MemoryStream();
                await input.CopyToAsync(memory, cancellationToken);
                memory.Position = 0;
                using var peReader = new PEReader(memory);
                if (!peReader.HasMetadata)
                {
                    throw new BadImageFormatException("The assembly does not contain CLI metadata.");
                }

                symbols.AddRange(ReadSymbols(peReader.GetMetadataReader(), acquisition, assemblyArtifact));
                inspectedAssemblies++;
            }
            catch (Exception exception) when (exception is IOException or BadImageFormatException or InvalidDataException or ArgumentException)
            {
                throw new InvalidDataException($"Unable to extract assembly metadata for package {acquisition.Manifest.Package.Id} {acquisition.Manifest.PackageVersion} from '{assemblyArtifact.RelativePath}' (TFM '{assemblyArtifact.TargetFramework}').", exception);
            }

            var xmlArtifact = acquisition.Manifest.Artifacts.Single(a => a.Kind == ArtifactKind.XmlDocumentation && a.TargetFramework == assemblyArtifact.TargetFramework);
            var platform = GetPlatform(assemblyArtifact.TargetFramework);
            var nativeArtifacts = acquisition.Manifest.Artifacts
                .Where(a => a.Kind == ArtifactKind.NativeLibrary && IsNativeForPlatform(a.RelativePath, platform))
                .ToArray();
            var sourceArtifacts = acquisition.Manifest.Artifacts
                .Where(a => a.TargetFramework == assemblyArtifact.TargetFramework && a.Kind is ArtifactKind.ManagedSource or ArtifactKind.NativeSource or ArtifactKind.NativeHeader)
                .ToArray();
            AddPlatformEvidence(platformEvidence, acquisition, assemblyArtifact, platform, nativeArtifacts, sourceArtifacts);
            AddSymbolPlatformEvidence(platformEvidence, acquisition, symbols.Where(symbol => symbol.TargetFramework == assemblyArtifact.TargetFramework), platform, assemblyArtifact, nativeArtifacts, sourceArtifacts);
            try
            {
                await using var input = entries[xmlArtifact.RelativePath].Open();
                using var reader = XmlReader.Create(input, new XmlReaderSettings { Async = true, DtdProcessing = DtdProcessing.Prohibit });
                var document = await XDocument.LoadAsync(reader, LoadOptions.PreserveWhitespace, cancellationToken);
                documentation.AddRange(ReadDocumentation(document, acquisition, xmlArtifact));
            }
            catch (Exception exception) when (exception is IOException or XmlException or InvalidOperationException)
            {
                throw new InvalidDataException($"Unable to parse XML documentation for package {acquisition.Manifest.Package.Id} {acquisition.Manifest.PackageVersion} from '{xmlArtifact.RelativePath}' (TFM '{xmlArtifact.TargetFramework}').", exception);
            }
        }

        var byId = symbols.Where(s => s.DocumentationId is not null)
            .GroupBy(s => (s.TargetFramework, s.DocumentationId!))
            .ToDictionary(group => group.Key, group => group.Count() == 1 ? group.Single() : null);
        var associations = new List<DocumentationAssociation>();
        var unassociated = new List<DocumentationRecord>();
        foreach (var item in documentation)
        {
            if (byId.TryGetValue((item.TargetFramework, item.MemberId), out var symbol) && symbol is not null)
            {
                associations.Add(new DocumentationAssociation(item, symbol));
            }
            else
            {
                unassociated.Add(item);
            }
        }

        if (_sourceRepository is not null && sourceContents.Count > 0)
        {
            var repositoryInspection = new SourceInspection().InspectRepositoryMappingsDetailed(acquisition, sourceContents, _sourceRepository);
            relationships.AddRange(repositoryInspection.Relationships);
            sourceCoverage = sourceCoverage.Add(repositoryInspection.Coverage);
            sourceDiagnostics.AddRange(repositoryInspection.Diagnostics);
        }

        return new PackageExtractionResult(acquisition, symbols, documentation, associations, unassociated, relationships, lifecycleFacts, platformEvidence, sourceCoverage, sourceDiagnostics, metadataEnumerationCoverage: new MetadataEnumerationCoverage
        {
            ExpectedManagedAssemblies = primaryAssemblies.Length,
            InspectedManagedAssemblies = inspectedAssemblies,
            SymbolEnumerationCompleted = inspectedAssemblies == primaryAssemblies.Length
        });
    }

    private static void AddPlatformEvidence(ICollection<PlatformEvidence> evidence, PackageAcquisitionResult acquisition, PackageArtifact assembly, KnowledgePlatform platform, IReadOnlyList<PackageArtifact> nativeArtifacts, IReadOnlyList<PackageArtifact> sourceArtifacts)
    {
        var managedSources = sourceArtifacts.Where(a => a.Kind == ArtifactKind.ManagedSource).ToArray();
        var nativeSources = sourceArtifacts.Where(a => a.Kind == ArtifactKind.NativeSource).ToArray();
        var nativeHeaders = sourceArtifacts.Where(a => a.Kind == ArtifactKind.NativeHeader).ToArray();
        var artifacts = new[] { assembly.RelativePath }
            .Concat(sourceArtifacts.Select(a => a.RelativePath))
            .Concat(nativeArtifacts.Select(a => a.RelativePath))
            .Order(StringComparer.Ordinal)
            .ToArray();
        var managedStatus = PlatformArtifactStatus.Present;
        var nativeStatus = nativeArtifacts.Count == 0 ? PlatformArtifactStatus.Unknown : PlatformArtifactStatus.Present;
        var sourceStatus = managedSources.Length == 0 ? PlatformArtifactStatus.Unknown : PlatformArtifactStatus.Present;
        var wrapperStatus = nativeSources.Length == 0 ? PlatformArtifactStatus.Unknown : PlatformArtifactStatus.Present;
        var headerStatus = nativeHeaders.Length == 0 ? PlatformArtifactStatus.Unknown : PlatformArtifactStatus.Present;
        var confidence = nativeArtifacts.Count > 0 && managedSources.Length > 0 && nativeSources.Length > 0 && nativeHeaders.Length > 0 ? KnowledgeConfidence.Confirmed : KnowledgeConfidence.Strong;
        var provenance = new FactProvenance(acquisition.Manifest.Identity, ProvenanceKind.PackageArtifact, assembly.RelativePath, sourceReference: string.Join(", ", artifacts), limitations: "Package and generated-source presence establish distribution evidence only; native runtime loading and execution were not validated.", targetFramework: assembly.TargetFramework, evidence: "Managed assembly, generated source, wrapper/header and native asset inventory.");
        evidence.Add(new PlatformEvidence(acquisition.Manifest.Identity, new KnowledgeNodeReference(KnowledgeNodeKind.ManagedSymbol, $"assembly:{assembly.RelativePath}"), platform, assembly.TargetFramework, null, managedStatus, nativeStatus, confidence, provenance, nativeArtifacts.Count == 0 ? "No matching native asset was identified in the acquired manifest." : null, sourceStatus, wrapperStatus, headerStatus, PlatformRuntimeStatus.NotValidated, artifacts));
    }

    private static void AddSymbolPlatformEvidence(ICollection<PlatformEvidence> evidence, PackageAcquisitionResult acquisition, IEnumerable<SymbolRepresentation> symbols, KnowledgePlatform platform, PackageArtifact assembly, IReadOnlyList<PackageArtifact> nativeArtifacts, IReadOnlyList<PackageArtifact> sourceArtifacts)
    {
        var managedSources = sourceArtifacts.Where(a => a.Kind == ArtifactKind.ManagedSource).ToArray();
        var nativeSources = sourceArtifacts.Where(a => a.Kind == ArtifactKind.NativeSource).ToArray();
        var nativeHeaders = sourceArtifacts.Where(a => a.Kind == ArtifactKind.NativeHeader).ToArray();
        foreach (var symbol in symbols.Where(symbol => symbol.Identity.Kind != SymbolKind.Namespace))
        {
            var matchingManagedSources = managedSources.Where(source => SymbolBelongsToSource(symbol.Identity.QualifiedName, source.RelativePath)).ToArray();
            var artifacts = matchingManagedSources.Select(a => a.RelativePath)
                .Concat(nativeSources.Select(a => a.RelativePath))
                .Concat(nativeHeaders.Select(a => a.RelativePath))
                .Concat(nativeArtifacts.Select(a => a.RelativePath))
                .Order(StringComparer.Ordinal)
                .ToArray();
            var limitations = new List<string>();
            if (matchingManagedSources.Length == 0) limitations.Add("No generated source file could be associated with this managed symbol.");
            if (nativeArtifacts.Count == 0) limitations.Add("Native distribution artifact was not identified for this platform.");
            limitations.Add("Static source inspection does not validate native runtime loading or execution.");
            var provenance = new FactProvenance(acquisition.Manifest.Identity, ProvenanceKind.PackageArtifact, symbol.SourceArtifact?.RelativePath, symbol.Identity, sourceReference: artifacts.Length == 0 ? assembly.RelativePath : string.Join(", ", artifacts), limitations: string.Join(" ", limitations), targetFramework: symbol.TargetFramework, evidence: matchingManagedSources.Length == 0 ? "Public managed symbol was found in the platform assembly; no type-specific generated source path matched it." : "Managed symbol name was matched to its generated source file path; wrapper and native assets remain separately evidenced.");
            var confidence = matchingManagedSources.Length > 0 && nativeArtifacts.Count > 0 && nativeSources.Length > 0 && nativeHeaders.Length > 0 ? KnowledgeConfidence.Confirmed : KnowledgeConfidence.Strong;
            evidence.Add(new PlatformEvidence(acquisition.Manifest.Identity, new KnowledgeNodeReference(KnowledgeNodeKind.ManagedSymbol, symbol.Identity.QualifiedName), platform, assembly.TargetFramework, null, PlatformArtifactStatus.Present, nativeArtifacts.Count == 0 ? PlatformArtifactStatus.Unknown : PlatformArtifactStatus.Present, confidence, provenance, string.Join(" ", limitations), matchingManagedSources.Length == 0 ? PlatformArtifactStatus.Unknown : PlatformArtifactStatus.Present, nativeSources.Length == 0 ? PlatformArtifactStatus.Unknown : PlatformArtifactStatus.Present, nativeHeaders.Length == 0 ? PlatformArtifactStatus.Unknown : PlatformArtifactStatus.Present, PlatformRuntimeStatus.NotValidated, artifacts));
        }
    }

    private static bool SymbolBelongsToSource(string qualifiedName, string sourcePath)
    {
        var typeName = Path.GetFileNameWithoutExtension(sourcePath);
        return qualifiedName.Equals(typeName, StringComparison.Ordinal) ||
            qualifiedName.Contains($".{typeName}.", StringComparison.Ordinal) ||
            qualifiedName.EndsWith($".{typeName}", StringComparison.Ordinal);
    }

    private static bool IsPrimaryLibAssembly(PackageArtifact artifact)
    {
        var parts = artifact.RelativePath.Split('/');
        return parts.Length == 3 && parts[0].Equals("lib", StringComparison.OrdinalIgnoreCase) &&
            parts[2].Equals("libpjsua2maui.dll", StringComparison.OrdinalIgnoreCase);
    }

    private static KnowledgePlatform GetPlatform(string? targetFramework)
    {
        if (targetFramework?.Contains("android", StringComparison.OrdinalIgnoreCase) == true) return KnowledgePlatform.Android;
        if (targetFramework?.Contains("ios", StringComparison.OrdinalIgnoreCase) == true) return KnowledgePlatform.iOS;
        if (targetFramework?.Contains("maccatalyst", StringComparison.OrdinalIgnoreCase) == true) return KnowledgePlatform.MacCatalyst;
        if (targetFramework?.Contains("windows", StringComparison.OrdinalIgnoreCase) == true) return KnowledgePlatform.Windows;
        return KnowledgePlatform.Unknown;
    }

    private static bool IsNativeForPlatform(string path, KnowledgePlatform platform)
    {
        return platform switch
        {
            KnowledgePlatform.Android => path.StartsWith("runtimes/android-", StringComparison.OrdinalIgnoreCase),
            KnowledgePlatform.iOS => path.StartsWith("runtimes/ios", StringComparison.OrdinalIgnoreCase),
            KnowledgePlatform.MacCatalyst => path.StartsWith("runtimes/maccatalyst-", StringComparison.OrdinalIgnoreCase),
            KnowledgePlatform.Windows => path.StartsWith("runtimes/win-", StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static IEnumerable<SymbolRepresentation> ReadSymbols(MetadataReader reader, PackageAcquisitionResult acquisition, PackageArtifact artifact)
    {
        var namespaces = new HashSet<string>(StringComparer.Ordinal);
        foreach (var handle in reader.TypeDefinitions)
        {
            var definition = reader.GetTypeDefinition(handle);
            if (!IsPublicType(reader, handle, definition))
            {
                continue;
            }

            var typeName = GetTypeName(reader, handle);
            var namespaceName = reader.GetString(definition.Namespace);
            if (!string.IsNullOrEmpty(namespaceName) && namespaces.Add(namespaceName))
            {
                yield return Create(acquisition, artifact, namespaceName, SymbolKind.Namespace, $"N:{namespaceName}", namespaceName);
            }

            var typeKind = GetTypeKind(reader, definition);
            yield return Create(acquisition, artifact, typeName, typeKind, $"T:{typeName}", typeName);

            foreach (var methodHandle in definition.GetMethods())
            {
                var method = reader.GetMethodDefinition(methodHandle);
                if (!IsPublicMethod(method.Attributes))
                {
                    continue;
                }

                var methodName = reader.GetString(method.Name);
                var signature = ReadMethodSignature(reader, method);
                var parameters = signature.Parameters;
                var parameterTypes = string.Join(",", parameters.Select(p => p.Type));
                var xmlName = methodName is ".ctor" or ".cctor" ? "#ctor" : methodName;
                var genericSuffix = method.GetGenericParameters().Count == 0 ? string.Empty : new string((char)96, 2) + method.GetGenericParameters().Count;
                var documentationId = $"M:{typeName}.{xmlName}{genericSuffix}({parameterTypes})";
                var qualifiedName = $"{typeName}.{xmlName}({parameterTypes})";
                var methodKind = methodName is ".ctor" or ".cctor"
                    ? SymbolKind.Constructor
                    : methodName.StartsWith("op_", StringComparison.Ordinal)
                        ? SymbolKind.Operator
                        : SymbolKind.Method;
                yield return Create(acquisition, artifact, qualifiedName, methodKind, documentationId, qualifiedName, parameters, signature.ReturnType, typeName, (method.Attributes & MethodAttributes.Static) != 0);
            }

            foreach (var propertyHandle in definition.GetProperties())
            {
                var property = reader.GetPropertyDefinition(propertyHandle);
                var name = reader.GetString(property.Name);
                if (!HasPublicAccessor(reader, property.GetAccessors()))
                {
                    continue;
                }

                var qualifiedName = $"{typeName}.{name}";
                var isStatic = IsPropertyStatic(reader, property.GetAccessors());
                yield return Create(acquisition, artifact, qualifiedName, SymbolKind.Property, $"P:{qualifiedName}", qualifiedName, declaringType: typeName, isStatic: isStatic);
            }

            foreach (var eventHandle in definition.GetEvents())
            {
                var @event = reader.GetEventDefinition(eventHandle);
                var name = reader.GetString(@event.Name);
                if (!HasPublicEventAccessor(reader, @event.GetAccessors()))
                {
                    continue;
                }

                var qualifiedName = $"{typeName}.{name}";
                var isStatic = IsEventStatic(reader, @event.GetAccessors());
                yield return Create(acquisition, artifact, qualifiedName, SymbolKind.Event, $"E:{qualifiedName}", qualifiedName, declaringType: typeName, isStatic: isStatic);
            }

            foreach (var fieldHandle in definition.GetFields())
            {
                var field = reader.GetFieldDefinition(fieldHandle);
                if ((field.Attributes & FieldAttributes.FieldAccessMask) != FieldAttributes.Public)
                {
                    continue;
                }

                var name = reader.GetString(field.Name);
                var qualifiedName = $"{typeName}.{name}";
                yield return Create(acquisition, artifact, qualifiedName, SymbolKind.Field, $"F:{qualifiedName}", qualifiedName, declaringType: typeName, isStatic: (field.Attributes & FieldAttributes.Static) != 0);
            }
        }
    }

    private static IEnumerable<DocumentationRecord> ReadDocumentation(XDocument document, PackageAcquisitionResult acquisition, PackageArtifact artifact)
    {
        foreach (var member in document.Root?.Element("members")?.Elements("member") ?? [])
        {
            var id = member.Attribute("name")?.Value;
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            var content = string.Concat(member.Nodes().Select(node => node.ToString(SaveOptions.DisableFormatting))).Trim();
            yield return new DocumentationRecord(acquisition.Manifest.Package, acquisition.Manifest.PackageVersion, id, content, artifact, artifact.TargetFramework);
        }
    }

    private static SymbolRepresentation Create(PackageAcquisitionResult acquisition, PackageArtifact artifact, string name, SymbolKind kind, string documentationId, string signature, IReadOnlyList<SymbolParameter>? parameters = null, string? returnType = null, string? declaringType = null, bool isStatic = false)
    {
        return new SymbolRepresentation(acquisition.Manifest.Package, acquisition.Manifest.PackageVersion, new SymbolIdentity(name, kind), artifact.TargetFramework, artifact, documentationId, signature, parameters, returnType, declaringType: declaringType, isStatic: isStatic);
    }

    private static bool IsPublicType(MetadataReader reader, TypeDefinitionHandle handle, TypeDefinition definition)
    {
        var visibility = definition.Attributes & TypeAttributes.VisibilityMask;
        if (visibility == TypeAttributes.Public)
        {
            return true;
        }

        return visibility == TypeAttributes.NestedPublic &&
            (definition.GetDeclaringType().IsNil || IsPublicType(reader, definition.GetDeclaringType(), reader.GetTypeDefinition(definition.GetDeclaringType())));
    }

    private static bool IsPublicMethod(MethodAttributes attributes)
    {
        var access = attributes & MethodAttributes.MemberAccessMask;
        return access is MethodAttributes.Public or MethodAttributes.Family or MethodAttributes.FamORAssem;
    }

    private static bool HasPublicAccessor(MetadataReader reader, PropertyAccessors accessors)
    {
        return IsPublic(reader, accessors.Getter) || IsPublic(reader, accessors.Setter);
    }

    private static bool HasPublicEventAccessor(MetadataReader reader, EventAccessors accessors)
    {
        return IsPublic(reader, accessors.Adder) || IsPublic(reader, accessors.Remover) || IsPublic(reader, accessors.Raiser);
    }

    private static bool IsPublic(MetadataReader reader, MethodDefinitionHandle handle)
    {
        return !handle.IsNil && IsPublicMethod(reader.GetMethodDefinition(handle).Attributes);
    }

    private static SymbolKind GetTypeKind(MetadataReader reader, TypeDefinition definition)
    {
        if ((definition.Attributes & TypeAttributes.Interface) != 0)
        {
            return SymbolKind.Interface;
        }

        var baseName = definition.BaseType.IsNil ? null : GetTypeName(reader, definition.BaseType);
        if (baseName is not null && HasRecordShape(reader, definition))
        {
            return SymbolKind.Record;
        }

        return baseName switch
        {
            "System.Enum" => SymbolKind.Enum,
            "System.ValueType" => SymbolKind.Struct,
            "System.MulticastDelegate" => SymbolKind.Delegate,
            _ => SymbolKind.Class
        };
    }

    private static bool HasRecordShape(MetadataReader reader, TypeDefinition definition)
    {
        return definition.GetProperties().Any(handle => reader.GetString(reader.GetPropertyDefinition(handle).Name) == "EqualityContract") ||
            definition.GetMethods().Any(handle => reader.GetString(reader.GetMethodDefinition(handle).Name) == "PrintMembers");
    }

    private static bool IsPropertyStatic(MetadataReader reader, PropertyAccessors accessors) =>
        IsStatic(reader, accessors.Getter) || IsStatic(reader, accessors.Setter);

    private static bool IsEventStatic(MetadataReader reader, EventAccessors accessors) =>
        IsStatic(reader, accessors.Adder) || IsStatic(reader, accessors.Remover) || IsStatic(reader, accessors.Raiser);

    private static bool IsStatic(MetadataReader reader, MethodDefinitionHandle handle) =>
        !handle.IsNil && (reader.GetMethodDefinition(handle).Attributes & MethodAttributes.Static) != 0;

    private static string GetTypeName(MetadataReader reader, TypeDefinitionHandle handle)
    {
        var definition = reader.GetTypeDefinition(handle);
        var name = reader.GetString(definition.Name);
        var genericCount = definition.GetGenericParameters().Count;
        if (genericCount > 0)
        {
            name += new string((char)96, 1) + genericCount;
        }
        var declaring = definition.GetDeclaringType();
        var prefix = declaring.IsNil ? reader.GetString(definition.Namespace) : GetTypeName(reader, declaring);
        return string.IsNullOrEmpty(prefix) ? name : $"{prefix}.{name}";
    }

    private static string GetTypeName(MetadataReader reader, EntityHandle handle)
    {
        return handle.Kind switch
        {
            HandleKind.TypeDefinition => GetTypeName(reader, (TypeDefinitionHandle)handle),
            HandleKind.TypeReference => GetTypeReferenceName(reader, (TypeReferenceHandle)handle),
            _ => "System.Object"
        };
    }

    private static string GetTypeReferenceName(MetadataReader reader, TypeReferenceHandle handle)
    {
        var reference = reader.GetTypeReference(handle);
        var name = reader.GetString(reference.Name);
        var namespaceName = reader.GetString(reference.Namespace);
        return string.IsNullOrEmpty(namespaceName) ? name : $"{namespaceName}.{name}";
    }

    private static (IReadOnlyList<SymbolParameter> Parameters, string ReturnType) ReadMethodSignature(MetadataReader reader, MethodDefinition method)
    {
        var blob = reader.GetBlobReader(method.Signature);
        var callingConvention = blob.ReadByte();
        if ((callingConvention & 0x10) != 0)
        {
            _ = blob.ReadCompressedInteger();
        }

        var parameterCount = blob.ReadCompressedInteger();
        var returnType = ReadSignatureType(reader, ref blob);
        var types = Enumerable.Range(0, parameterCount).Select(_ => ReadSignatureType(reader, ref blob)).ToArray();
        var names = method.GetParameters().Select(reader.GetParameter).Where(parameter => parameter.SequenceNumber > 0)
            .OrderBy(parameter => parameter.SequenceNumber).Select(parameter => reader.GetString(parameter.Name)).ToArray();
        var parameters = types.Select((type, index) => new SymbolParameter(index < names.Length ? names[index] : $"arg{index}", type)).ToArray();
        return (parameters, returnType);
    }

    private static string ReadSignatureType(MetadataReader reader, ref BlobReader blob)
    {
        var element = blob.ReadByte();
        return element switch
        {
            0x01 => "System.Void",
            0x02 => "System.Boolean",
            0x03 => "System.Char",
            0x04 => "System.SByte",
            0x05 => "System.Byte",
            0x06 => "System.Int16",
            0x07 => "System.UInt16",
            0x08 => "System.Int32",
            0x09 => "System.UInt32",
            0x0A => "System.Int64",
            0x0B => "System.UInt64",
            0x0C => "System.Single",
            0x0D => "System.Double",
            0x0E => "System.String",
            0x0F => $"{ReadSignatureType(reader, ref blob)}*",
            0x10 => $"{ReadSignatureType(reader, ref blob)}&",
            0x11 or 0x12 => GetTypeName(reader, MetadataTokens.EntityHandle(blob.ReadCompressedInteger())),
            0x13 => new string((char)96, 2) + blob.ReadCompressedInteger(),
            0x14 => ReadArrayType(reader, ref blob),
            0x15 => ReadGenericType(reader, ref blob),
            0x16 => "System.TypedReference",
            0x18 => "System.IntPtr",
            0x19 => "System.UIntPtr",
            0x1D => $"{ReadSignatureType(reader, ref blob)}[]",
            0x1E => "!!" + blob.ReadCompressedInteger(),
            _ => "System.Object"
        };
    }

    private static string ReadArrayType(MetadataReader reader, ref BlobReader blob)
    {
        var element = ReadSignatureType(reader, ref blob);
        var rank = blob.ReadCompressedInteger();
        var sizes = blob.ReadCompressedInteger();
        for (var index = 0; index < sizes; index++) _ = blob.ReadCompressedInteger();
        var lowerBounds = blob.ReadCompressedInteger();
        for (var index = 0; index < lowerBounds; index++) _ = blob.ReadCompressedSignedInteger();
        return rank == 1 ? $"{element}[]" : $"{element}[{new string(',', rank - 1)}]";
    }

    private static string ReadGenericType(MetadataReader reader, ref BlobReader blob)
    {
        _ = blob.ReadByte();
        var type = GetTypeName(reader, MetadataTokens.EntityHandle(blob.ReadCompressedInteger()));
        var count = blob.ReadCompressedInteger();
        var arguments = new string[count];
        for (var index = 0; index < count; index++)
        {
            arguments[index] = ReadSignatureType(reader, ref blob);
        }
        return $"{type}<{string.Join(",", arguments)}>";
    }
}
