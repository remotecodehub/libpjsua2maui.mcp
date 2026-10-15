namespace libpjsua2maui.mcp.Knowledge;

public static class PackageSymbolInventoryBuilder
{
    public static PackageSymbolInventory Build(PackageExtractionResult extraction)
    {
        ArgumentNullException.ThrowIfNull(extraction);

        var xmlByTfm = extraction.Documentation
            .GroupBy(document => document.TargetFramework ?? string.Empty, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Select(document => document.MemberId).ToHashSet(StringComparer.Ordinal), StringComparer.Ordinal);
        var sourceArtifacts = extraction.Acquisition.Manifest.Artifacts
            .Where(artifact => artifact.Kind == ArtifactKind.ManagedSource)
            .ToArray();

        var items = extraction.Symbols
            .Where(symbol => symbol.Identity.Kind != SymbolKind.Namespace)
            .GroupBy(symbol => CanonicalKey(symbol), StringComparer.Ordinal)
            .Select(group => CreateItem(group, xmlByTfm, sourceArtifacts))
            .OrderBy(item => item.Identity, StringComparer.Ordinal)
            .ToArray();

        return new PackageSymbolInventory(items);
    }

    private static PackageInventoryItem CreateItem(
        IGrouping<string, SymbolRepresentation> group,
        IReadOnlyDictionary<string, HashSet<string>> xmlByTfm,
        IReadOnlyList<PackageArtifact> sourceArtifacts)
    {
        var first = group.OrderBy(symbol => symbol.TargetFramework, StringComparer.Ordinal).ThenBy(symbol => symbol.SourceArtifact?.RelativePath, StringComparer.Ordinal).First();
        var evidence = group.Select(symbol =>
        {
            var source = sourceArtifacts.FirstOrDefault(artifact => artifact.TargetFramework == symbol.TargetFramework && MatchesSource(symbol, artifact));
            var hasXml = symbol.DocumentationId is not null && xmlByTfm.TryGetValue(symbol.TargetFramework ?? string.Empty, out var ids) && ids.Contains(symbol.DocumentationId);
            return new InventoryEvidence(symbol.TargetFramework, symbol.SourceArtifact?.RelativePath, source?.RelativePath, hasXml);
        }).ToArray();
        var hasSource = evidence.Any(item => item.SourcePath is not null);
        var hasXmlDocumentation = evidence.Any(item => item.HasXmlDocumentation);
        var status = hasSource && hasXmlDocumentation ? InspectionStatus.Inspected : InspectionStatus.Partial;
        var reason = status == InspectionStatus.Partial
            ? $"Missing evidence: {(hasSource ? string.Empty : "generated managed source")}{(!hasSource && !hasXmlDocumentation ? " and " : string.Empty)}{(hasXmlDocumentation ? string.Empty : "XML documentation")} for one or more TFM representations."
            : null;
        return new PackageInventoryItem(
            first.Package,
            first.PackageVersion,
            "libpjsua2maui",
            first,
            status,
            ClassifyOrigin(first),
            evidence,
            first.DocumentationId,
            reason);
    }

    private static string CanonicalKey(SymbolRepresentation symbol) => $"{symbol.Identity.Kind}|{symbol.Identity.QualifiedName}|{symbol.Signature}";

    private static bool MatchesSource(SymbolRepresentation symbol, PackageArtifact source)
    {
        var fileName = Path.GetFileNameWithoutExtension(source.RelativePath);
        var declaringName = symbol.DeclaringType.Split('.').Last().Split('`')[0];
        return (source.TargetFramework is null || source.TargetFramework == symbol.TargetFramework) &&
            fileName.Equals(declaringName, StringComparison.Ordinal);
    }

    private static SymbolOrigin ClassifyOrigin(SymbolRepresentation symbol) => symbol.Identity.Kind switch
    {
        SymbolKind.Enum => SymbolOrigin.SwigEnum,
        SymbolKind.Delegate => SymbolOrigin.SwigDelegate,
        _ when symbol.DeclaringType.Contains("SWIG", StringComparison.OrdinalIgnoreCase) || symbol.DeclaringType.Contains("PINVOKE", StringComparison.OrdinalIgnoreCase) => SymbolOrigin.GeneratedHelper,
        _ => SymbolOrigin.UnknownOrigin
    };
}
