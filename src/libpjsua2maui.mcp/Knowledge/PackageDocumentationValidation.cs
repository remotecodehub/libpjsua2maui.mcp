using System.Collections.ObjectModel;

namespace libpjsua2maui.mcp.Knowledge;

/// <summary>Reports XML documentation association for every package TFM containing the managed assembly.</summary>
public sealed record PackageDocumentationValidation
{
    public PackageDocumentationValidation(
        PackageVersion packageVersion,
        IReadOnlyDictionary<string, string> expectedXmlPaths,
        IReadOnlyCollection<string> documentedTfms,
        IReadOnlyCollection<string> missingDocumentationTfms)
    {
        PackageVersion = packageVersion ?? throw new ArgumentNullException(nameof(packageVersion));
        ExpectedXmlPaths = Copy(expectedXmlPaths);
        DocumentedTfms = Array.AsReadOnly(documentedTfms.Order(StringComparer.Ordinal).ToArray());
        MissingDocumentationTfms = Array.AsReadOnly(missingDocumentationTfms.Order(StringComparer.Ordinal).ToArray());
        IsValid = MissingDocumentationTfms.Count == 0 && ExpectedXmlPaths.Count > 0;
    }

    public PackageVersion PackageVersion { get; }

    public IReadOnlyDictionary<string, string> ExpectedXmlPaths { get; }

    public IReadOnlyCollection<string> DocumentedTfms { get; }

    public IReadOnlyCollection<string> MissingDocumentationTfms { get; }

    public bool IsValid { get; }

    private static ReadOnlyDictionary<string, string> Copy(IReadOnlyDictionary<string, string> values)
    {
        ArgumentNullException.ThrowIfNull(values);
        return new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(values, StringComparer.Ordinal));
    }
}
