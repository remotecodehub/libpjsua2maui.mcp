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
            .OrderBy(artifact => artifact.RelativePath, StringComparer.Ordinal)
            .ToArray();
        var targetFrameworks = extraction.Acquisition.Manifest.Artifacts
            .Where(artifact => artifact.Kind == ArtifactKind.ManagedAssembly && artifact.TargetFramework is not null)
            .Select(artifact => artifact.TargetFramework!)
            .Distinct(StringComparer.Ordinal)
            .Order(StringComparer.Ordinal)
            .ToArray();

        var items = extraction.Symbols
            .Where(symbol => symbol.Identity.Kind != SymbolKind.Namespace)
            .GroupBy(CanonicalKey, StringComparer.Ordinal)
            .Select(group => CreateItem(group, extraction, xmlByTfm, sourceArtifacts, targetFrameworks))
            .OrderBy(item => item.Identity, StringComparer.Ordinal)
            .ToArray();

        return new PackageSymbolInventory(items, extraction.MetadataEnumerationCoverage);
    }

    private static PackageInventoryItem CreateItem(
        IGrouping<string, SymbolRepresentation> group,
        PackageExtractionResult extraction,
        IReadOnlyDictionary<string, HashSet<string>> xmlByTfm,
        IReadOnlyList<PackageArtifact> sourceArtifacts,
        IReadOnlyList<string> targetFrameworks)
    {
        var first = group.OrderBy(symbol => symbol.TargetFramework, StringComparer.Ordinal).ThenBy(symbol => symbol.SourceArtifact?.RelativePath, StringComparer.Ordinal).First();
        var evidence = group.Select(symbol => CreateEvidence(symbol, extraction, xmlByTfm, sourceArtifacts, targetFrameworks)).ToArray();
        var complete = evidence.All(item => item.SourceStatus == EvidenceAssociationStatus.Proven && item.XmlStatus == EvidenceAssociationStatus.Proven);
        var status = complete ? InspectionStatus.Inspected : InspectionStatus.Partial;
        var reason = complete ? null : string.Join(" ", evidence.Where(item => item.EvidenceReason is not null).Select(item => item.EvidenceReason).Distinct(StringComparer.Ordinal));
        return new PackageInventoryItem(first.Package, first.PackageVersion, "libpjsua2maui", first, status, ClassifyOrigin(first), evidence, first.DocumentationId, reason);
    }

    private static InventoryEvidence CreateEvidence(
        SymbolRepresentation symbol,
        PackageExtractionResult extraction,
        IReadOnlyDictionary<string, HashSet<string>> xmlByTfm,
        IReadOnlyList<PackageArtifact> sourceArtifacts,
        IReadOnlyList<string> targetFrameworks)
    {
        var matchingNames = sourceArtifacts.Where(artifact => HasMatchingDeclaredType(symbol, artifact)).ToArray();
        var candidates = matchingNames.Where(artifact => AppliesToTargetFramework(artifact, symbol.TargetFramework, targetFrameworks)).ToArray();
        var sourceStatus = candidates.Length switch
        {
            0 when matchingNames.Length > 0 => EvidenceAssociationStatus.Ambiguous,
            1 => EvidenceAssociationStatus.Proven,
            _ when candidates.Length > 1 => EvidenceAssociationStatus.Ambiguous,
            _ => EvidenceAssociationStatus.Missing
        };
        var source = sourceStatus == EvidenceAssociationStatus.Proven ? candidates[0] : null;
        var xmlStatus = symbol.DocumentationId is not null && xmlByTfm.TryGetValue(symbol.TargetFramework ?? string.Empty, out var ids) && ids.Contains(symbol.DocumentationId)
            ? EvidenceAssociationStatus.Proven
            : EvidenceAssociationStatus.Missing;
        var reasons = new List<string>();
        if (sourceStatus == EvidenceAssociationStatus.Missing) reasons.Add("No generated managed source artifact matched this symbol and TFM.");
        if (sourceStatus == EvidenceAssociationStatus.Ambiguous) reasons.Add("More than one source association or a platform/TFM ambiguity matched this symbol.");
        if (xmlStatus == EvidenceAssociationStatus.Missing) reasons.Add("XML documentation is absent for this symbol and TFM.");
        var provenance = source is null ? null : new FactProvenance(extraction.Acquisition.Manifest.Identity, ProvenanceKind.PackageArtifact, source.RelativePath, symbol.Identity, targetFramework: source.TargetFramework ?? symbol.TargetFramework, evidence: "Source artifact was associated by explicit TFM or an unambiguous platform path and declaring-type filename.");
        return new InventoryEvidence(symbol.TargetFramework, symbol.SourceArtifact?.RelativePath, source?.RelativePath, xmlStatus == EvidenceAssociationStatus.Proven, sourceStatus, xmlStatus, string.Join(" ", reasons), provenance);
    }

    private static bool HasMatchingDeclaredType(SymbolRepresentation symbol, PackageArtifact artifact)
    {
        var fileName = Path.GetFileNameWithoutExtension(artifact.RelativePath);
        var declaringName = symbol.DeclaringType.Split('.').Last().Split('`')[0];
        return fileName.Equals(declaringName, StringComparison.Ordinal);
    }

    private static bool AppliesToTargetFramework(PackageArtifact artifact, string? targetFramework, IReadOnlyList<string> allTargetFrameworks)
    {
        if (artifact.TargetFramework is not null)
        {
            return artifact.TargetFramework.Equals(targetFramework, StringComparison.Ordinal);
        }

        var platform = PlatformFromPath(artifact.RelativePath);
        if (platform is not null)
        {
            return targetFramework?.Contains(platform, StringComparison.OrdinalIgnoreCase) == true;
        }

        return allTargetFrameworks.Count == 1 && allTargetFrameworks[0].Equals(targetFramework, StringComparison.Ordinal);
    }

    private static string? PlatformFromPath(string path)
    {
        if (path.Contains("/Platforms/Android/", StringComparison.OrdinalIgnoreCase)) return "android";
        if (path.Contains("/Platforms/iOS/", StringComparison.OrdinalIgnoreCase)) return "ios";
        if (path.Contains("/Platforms/MacCatalyst/", StringComparison.OrdinalIgnoreCase)) return "maccatalyst";
        if (path.Contains("/Platforms/Windows/", StringComparison.OrdinalIgnoreCase)) return "windows";
        return null;
    }

    private static string CanonicalKey(SymbolRepresentation symbol) => $"{symbol.Identity.Kind}|{symbol.Identity.QualifiedName}|{symbol.Signature}";

    private static SymbolOrigin ClassifyOrigin(SymbolRepresentation symbol) => symbol.Identity.Kind switch
    {
        SymbolKind.Enum => SymbolOrigin.SwigEnum,
        SymbolKind.Delegate => SymbolOrigin.SwigDelegate,
        _ when symbol.DeclaringType.Contains("SWIGTYPE_", StringComparison.Ordinal) => SymbolOrigin.SwigOpaqueType,
        _ when symbol.DeclaringType.Contains("PINVOKE", StringComparison.OrdinalIgnoreCase) => SymbolOrigin.GeneratedHelper,
        _ when symbol.SourceArtifact?.RelativePath.Contains("/pjsua2/", StringComparison.OrdinalIgnoreCase) == true => SymbolOrigin.SwigManagedWrapper,
        _ => SymbolOrigin.UnknownOrigin
    };
}
