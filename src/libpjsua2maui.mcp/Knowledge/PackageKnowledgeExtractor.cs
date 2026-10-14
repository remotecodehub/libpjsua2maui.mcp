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
    public async Task<PackageExtractionResult> ExtractAsync(PackageAcquisitionResult acquisition, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(acquisition);
        var symbols = new List<SymbolRepresentation>();
        var documentation = new List<DocumentationRecord>();

        await using var packageStream = new FileStream(acquisition.ArtifactPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        using var archive = new System.IO.Compression.ZipArchive(packageStream, System.IO.Compression.ZipArchiveMode.Read);
        var entries = archive.Entries.ToDictionary(entry => entry.FullName.Replace('\\', '/'), StringComparer.OrdinalIgnoreCase);

        foreach (var assemblyArtifact in acquisition.Manifest.Artifacts.Where(a => a.Kind == ArtifactKind.ManagedAssembly).OrderBy(a => a.TargetFramework, StringComparer.Ordinal))
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
            }
            catch (Exception exception) when (exception is IOException or BadImageFormatException or InvalidDataException or ArgumentException)
            {
                throw new InvalidDataException($"Unable to extract assembly metadata for package {acquisition.Manifest.Package.Id} {acquisition.Manifest.PackageVersion} from '{assemblyArtifact.RelativePath}' (TFM '{assemblyArtifact.TargetFramework}').", exception);
            }

            var xmlArtifact = acquisition.Manifest.Artifacts.Single(a => a.Kind == ArtifactKind.XmlDocumentation && a.TargetFramework == assemblyArtifact.TargetFramework);
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

        return new PackageExtractionResult(acquisition, symbols, documentation, associations, unassociated);
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
                yield return Create(acquisition, artifact, qualifiedName, methodName is ".ctor" or ".cctor" ? SymbolKind.Constructor : SymbolKind.Method, documentationId, qualifiedName, parameters, signature.ReturnType);
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
                yield return Create(acquisition, artifact, qualifiedName, SymbolKind.Property, $"P:{qualifiedName}", qualifiedName);
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
                yield return Create(acquisition, artifact, qualifiedName, SymbolKind.Event, $"E:{qualifiedName}", qualifiedName);
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
                yield return Create(acquisition, artifact, qualifiedName, SymbolKind.Field, $"F:{qualifiedName}", qualifiedName);
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

    private static SymbolRepresentation Create(PackageAcquisitionResult acquisition, PackageArtifact artifact, string name, SymbolKind kind, string documentationId, string signature, IReadOnlyList<SymbolParameter>? parameters = null, string? returnType = null)
    {
        return new SymbolRepresentation(acquisition.Manifest.Package, acquisition.Manifest.PackageVersion, new SymbolIdentity(name, kind), artifact.TargetFramework, artifact, documentationId, signature, parameters, returnType);
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
        return baseName switch
        {
            "System.Enum" => SymbolKind.Enum,
            "System.ValueType" => SymbolKind.Struct,
            "System.MulticastDelegate" => SymbolKind.Delegate,
            _ => SymbolKind.Class
        };
    }

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
