namespace libpjsua2maui.mcp.Knowledge;

public enum InspectionStatus
{
    NotInspected,
    Inspected,
    Mapped,
    Partial,
    Unknown,
    NotApplicable,
    Unparsed
}

public enum SymbolOrigin
{
    SwigManagedWrapper,
    SwigOpaqueType,
    SwigEnum,
    SwigDelegate,
    NativeBackedType,
    ManagedOnly,
    GeneratedHelper,
    UnknownOrigin
}

public sealed record InventoryEvidence
{
    public InventoryEvidence(string? targetFramework, string? assemblyPath, string? sourcePath, bool hasXmlDocumentation)
    {
        TargetFramework = targetFramework;
        AssemblyPath = assemblyPath;
        SourcePath = sourcePath;
        HasXmlDocumentation = hasXmlDocumentation;
    }

    public string? TargetFramework { get; }
    public string? AssemblyPath { get; }
    public string? SourcePath { get; }
    public bool HasXmlDocumentation { get; }
}

public sealed record PackageInventoryItem
{
    public PackageInventoryItem(
        PackageIdentity package,
        PackageVersion packageVersion,
        string assembly,
        SymbolRepresentation symbol,
        InspectionStatus status,
        SymbolOrigin origin,
        IReadOnlyList<InventoryEvidence> evidence,
        string? xmlDocumentationIdentity,
        string? statusReason = null)
    {
        Package = package;
        PackageVersion = packageVersion;
        Assembly = assembly;
        Symbol = symbol;
        Status = status;
        Origin = origin;
        Evidence = Array.AsReadOnly((evidence ?? []).OrderBy(item => item.TargetFramework, StringComparer.Ordinal).ThenBy(item => item.AssemblyPath, StringComparer.Ordinal).ToArray());
        XmlDocumentationIdentity = xmlDocumentationIdentity;
        StatusReason = statusReason;
    }

    public PackageIdentity Package { get; }
    public PackageVersion PackageVersion { get; }
    public string Assembly { get; }
    public SymbolRepresentation Symbol { get; }
    public InspectionStatus Status { get; }
    public SymbolOrigin Origin { get; }
    public IReadOnlyList<InventoryEvidence> Evidence { get; }
    public string? XmlDocumentationIdentity { get; }
    public string? StatusReason { get; }
    public string Identity => $"{Package.Id}@{PackageVersion}|{Assembly}|{Symbol.Identity.Kind}|{Symbol.Identity.QualifiedName}|{Symbol.Signature}";
}

public sealed record InventoryCounters
{
    public int TotalTypes { get; init; }
    public int TotalClasses { get; init; }
    public int TotalRecords { get; init; }
    public int TotalStructs { get; init; }
    public int TotalEnums { get; init; }
    public int TotalDelegates { get; init; }
    public int TotalInterfaces { get; init; }
    public int TotalOtherTypes { get; init; }
    public int TotalMembers { get; init; }
    public int TotalConstructors { get; init; }
    public int TotalMethods { get; init; }
    public int TotalProperties { get; init; }
    public int TotalFields { get; init; }
    public int TotalEvents { get; init; }
    public int TotalOperators { get; init; }
    public int TotalStaticMembers { get; init; }
    public int TypesInspected { get; init; }
    public int MembersInspected { get; init; }
    public int TypesWithoutSource { get; init; }
    public int MembersWithoutSource { get; init; }
    public int TypesWithoutXmlDocumentation { get; init; }
    public int MembersWithoutXmlDocumentation { get; init; }
}

public sealed record InventoryCompletenessReport(InventoryCounters Counters)
{
    public bool InventoryComplete => Counters.TotalTypes == Counters.TypesInspected && Counters.TotalMembers == Counters.MembersInspected;
    public decimal TypeInspectionPercentage => Percentage(Counters.TypesInspected, Counters.TotalTypes);
    public decimal MemberInspectionPercentage => Percentage(Counters.MembersInspected, Counters.TotalMembers);

    public string ToText(PackageIdentity? package, PackageVersion? packageVersion, IReadOnlyList<string> targetFrameworks)
    {
        return string.Join(Environment.NewLine,
            $"Package: {package?.Id ?? "unknown"}",
            $"Version: {packageVersion?.Value ?? "unknown"}",
            $"TFMs: {string.Join(", ", targetFrameworks)}",
            $"Total types: {Counters.TotalTypes}",
            $"Total members: {Counters.TotalMembers}",
            $"Types inspected: {Counters.TypesInspected}",
            $"Members inspected: {Counters.MembersInspected}",
            $"Types without source: {Counters.TypesWithoutSource}",
            $"Members without source: {Counters.MembersWithoutSource}",
            $"Types without XML documentation: {Counters.TypesWithoutXmlDocumentation}",
            $"Members without XML documentation: {Counters.MembersWithoutXmlDocumentation}",
            $"Inventory completeness: types {TypeInspectionPercentage:0.##}%; members {MemberInspectionPercentage:0.##}%");
    }

    private static decimal Percentage(int numerator, int denominator) => denominator == 0 ? 100m : numerator * 100m / denominator;
}

public sealed record PackageSymbolInventory
{
    public PackageSymbolInventory(IReadOnlyList<PackageInventoryItem> items)
    {
        Items = Array.AsReadOnly((items ?? []).OrderBy(item => item.Identity, StringComparer.Ordinal).ToArray());
        Package = Items.FirstOrDefault()?.Package;
        PackageVersion = Items.FirstOrDefault()?.PackageVersion;
        TargetFrameworks = Items.SelectMany(item => item.Evidence).Select(evidence => evidence.TargetFramework).Where(value => value is not null).Select(value => value!).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        Counters = CalculateCounters(Items);
        Completeness = new InventoryCompletenessReport(Counters);
    }

    public IReadOnlyList<PackageInventoryItem> Items { get; }
    public PackageIdentity? Package { get; }
    public PackageVersion? PackageVersion { get; }
    public IReadOnlyList<string> TargetFrameworks { get; }
    public InventoryCounters Counters { get; }
    public InventoryCompletenessReport Completeness { get; }
    public string TextReport => Completeness.ToText(Package, PackageVersion, TargetFrameworks);

    private static InventoryCounters CalculateCounters(IReadOnlyList<PackageInventoryItem> items)
    {
        var types = items.Where(item => IsType(item.Symbol.Identity.Kind)).ToArray();
        var members = items.Where(item => !IsType(item.Symbol.Identity.Kind)).ToArray();
        return new InventoryCounters
        {
            TotalTypes = types.Length,
            TotalClasses = types.Count(item => item.Symbol.Identity.Kind == SymbolKind.Class),
            TotalRecords = types.Count(item => item.Symbol.Identity.Kind == SymbolKind.Record),
            TotalStructs = types.Count(item => item.Symbol.Identity.Kind == SymbolKind.Struct),
            TotalEnums = types.Count(item => item.Symbol.Identity.Kind == SymbolKind.Enum),
            TotalDelegates = types.Count(item => item.Symbol.Identity.Kind == SymbolKind.Delegate),
            TotalInterfaces = types.Count(item => item.Symbol.Identity.Kind == SymbolKind.Interface),
            TotalOtherTypes = types.Count(item => item.Symbol.Identity.Kind == SymbolKind.OtherType),
            TotalMembers = members.Length,
            TotalConstructors = members.Count(item => item.Symbol.Identity.Kind == SymbolKind.Constructor),
            TotalMethods = members.Count(item => item.Symbol.Identity.Kind == SymbolKind.Method),
            TotalProperties = members.Count(item => item.Symbol.Identity.Kind == SymbolKind.Property),
            TotalFields = members.Count(item => item.Symbol.Identity.Kind == SymbolKind.Field),
            TotalEvents = members.Count(item => item.Symbol.Identity.Kind == SymbolKind.Event),
            TotalOperators = members.Count(item => item.Symbol.Identity.Kind == SymbolKind.Operator),
            TotalStaticMembers = members.Count(item => item.Symbol.IsStatic),
            TypesInspected = types.Count(item => item.Status != InspectionStatus.NotInspected),
            MembersInspected = members.Count(item => item.Status != InspectionStatus.NotInspected),
            TypesWithoutSource = types.Count(item => item.Evidence.All(evidence => evidence.SourcePath is null)),
            MembersWithoutSource = members.Count(item => item.Evidence.All(evidence => evidence.SourcePath is null)),
            TypesWithoutXmlDocumentation = types.Count(item => item.Evidence.All(evidence => !evidence.HasXmlDocumentation)),
            MembersWithoutXmlDocumentation = members.Count(item => item.Evidence.All(evidence => !evidence.HasXmlDocumentation))
        };
    }

    private static bool IsType(SymbolKind kind) => kind is SymbolKind.Class or SymbolKind.Record or SymbolKind.Struct or SymbolKind.Enum or SymbolKind.Delegate or SymbolKind.Interface or SymbolKind.OtherType;
}
