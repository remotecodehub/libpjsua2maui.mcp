using System.Collections.ObjectModel;

namespace libpjsua2maui.mcp.Knowledge;

/// <summary>Contains assembly and XML documentation observations from one acquired package.</summary>
public sealed record PackageExtractionResult
{
    public PackageExtractionResult(
        PackageAcquisitionResult acquisition,
        IReadOnlyList<SymbolRepresentation> symbols,
        IReadOnlyList<DocumentationRecord> documentation,
        IReadOnlyList<DocumentationAssociation> associations,
        IReadOnlyList<DocumentationRecord> unassociatedDocumentation)
    {
        Acquisition = acquisition ?? throw new ArgumentNullException(nameof(acquisition));
        Symbols = Array.AsReadOnly((symbols ?? throw new ArgumentNullException(nameof(symbols))).ToArray());
        Documentation = Array.AsReadOnly((documentation ?? throw new ArgumentNullException(nameof(documentation))).ToArray());
        Associations = Array.AsReadOnly((associations ?? throw new ArgumentNullException(nameof(associations))).ToArray());
        UnassociatedDocumentation = Array.AsReadOnly((unassociatedDocumentation ?? throw new ArgumentNullException(nameof(unassociatedDocumentation))).ToArray());
        Errors = new ReadOnlyCollection<string>([]);
    }

    public PackageAcquisitionResult Acquisition { get; }
    public IReadOnlyList<SymbolRepresentation> Symbols { get; }
    public IReadOnlyList<DocumentationRecord> Documentation { get; }
    public IReadOnlyList<DocumentationAssociation> Associations { get; }
    public IReadOnlyList<DocumentationRecord> UnassociatedDocumentation { get; }
    public IReadOnlyList<string> Errors { get; }
}
