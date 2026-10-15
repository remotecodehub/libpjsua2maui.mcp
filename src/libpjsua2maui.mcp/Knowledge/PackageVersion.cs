using System.Globalization;

namespace libpjsua2maui.mcp.Knowledge;

/// <summary>
/// Represents a NuGet-compatible package version and its precedence.
/// </summary>
public sealed record PackageVersion : IComparable<PackageVersion>
{
    private static readonly PackageVersion MinimumSupported = new("2.17.0.5", skipMinimumValidation: true);
    private readonly IReadOnlyList<VersionIdentifier> _releaseIdentifiers = [];
    private readonly IReadOnlyList<int> _numericParts = [];

    private PackageVersion(string value, bool skipMinimumValidation)
    {
        var separator = value.IndexOf('-');
        var release = separator < 0 ? value : value[..separator];
        var prerelease = separator < 0 ? null : value[(separator + 1)..];

        if (!TryParseNumericParts(release, out var numericParts) ||
            prerelease is not null && !TryParsePrerelease(prerelease, out _releaseIdentifiers))
        {
            throw new FormatException($"'{value}' is not a valid NuGet package version.");
        }

        if (prerelease is null)
        {
            _releaseIdentifiers = [];
        }

        _numericParts = numericParts.Concat(Enumerable.Repeat(0, 4 - numericParts.Count)).ToArray();
        Value = prerelease is null ? string.Join('.', _numericParts) : $"{string.Join('.', _numericParts)}-{prerelease}";

        if (!skipMinimumValidation && CompareTo(MinimumSupported) < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, $"Package versions before {MinimumSupported} are not supported.");
        }
    }

    /// <summary>
    /// The minimum package version supported by the knowledge model.
    /// </summary>
    public static PackageVersion Minimum => MinimumSupported;

    /// <summary>
    /// The normalized version text.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Parses and validates a supported package version.
    /// </summary>
    public static PackageVersion Parse(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return new PackageVersion(value, skipMinimumValidation: false);
    }

    /// <inheritdoc />
    public int CompareTo(PackageVersion? other)
    {
        if (other is null)
        {
            return 1;
        }

        for (var index = 0; index < Math.Max(_numericParts.Count, other._numericParts.Count); index++)
        {
            var left = index < _numericParts.Count ? _numericParts[index] : 0;
            var right = index < other._numericParts.Count ? other._numericParts[index] : 0;
            var comparison = left.CompareTo(right);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        if (_releaseIdentifiers.Count == 0 && other._releaseIdentifiers.Count == 0)
        {
            return 0;
        }

        if (_releaseIdentifiers.Count == 0)
        {
            return 1;
        }

        if (other._releaseIdentifiers.Count == 0)
        {
            return -1;
        }

        for (var index = 0; index < Math.Max(_releaseIdentifiers.Count, other._releaseIdentifiers.Count); index++)
        {
            if (index >= _releaseIdentifiers.Count)
            {
                return -1;
            }

            if (index >= other._releaseIdentifiers.Count)
            {
                return 1;
            }

            var comparison = _releaseIdentifiers[index].CompareTo(other._releaseIdentifiers[index]);
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return 0;
    }

    /// <inheritdoc />
    public override string ToString() => Value;

    public bool Equals(PackageVersion? other)
    {
        return other is not null && CompareTo(other) == 0;
    }

    public override int GetHashCode()
    {
        return StringComparer.Ordinal.GetHashCode(Value);
    }

    private static bool TryParseNumericParts(string value, out IReadOnlyList<int> parts)
    {
        var values = value.Split('.', StringSplitOptions.None);
        if (values is not [_, ..] || values.Length > 4)
        {
            parts = [];
            return false;
        }

        var parsed = new List<int>(values.Length);
        foreach (var item in values)
        {
            if (item.Length == 0 || item[0] == '+' ||
                item.Length > 1 && item[0] == '0' ||
                !int.TryParse(item, NumberStyles.None, CultureInfo.InvariantCulture, out var number) || number < 0)
            {
                parts = [];
                return false;
            }

            parsed.Add(number);
        }

        parts = parsed;
        return true;
    }

    private static bool TryParsePrerelease(string value, out IReadOnlyList<VersionIdentifier> identifiers)
    {
        var parsed = new List<VersionIdentifier>();
        var values = value.Split('.', StringSplitOptions.None);
        if (values is not [_, ..])
        {
            identifiers = [];
            return false;
        }

        foreach (var item in values)
        {
            if (item.Length == 0 || item.Any(character => !char.IsLetterOrDigit(character) && character != '-'))
            {
                identifiers = [];
                return false;
            }

            parsed.Add(VersionIdentifier.Parse(item));
        }

        identifiers = parsed;
        return true;
    }

    private sealed record VersionIdentifier(string Value, int? NumericValue) : IComparable<VersionIdentifier>
    {
        public static VersionIdentifier Parse(string value)
        {
            return int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
                ? new VersionIdentifier(value, number)
                : new VersionIdentifier(value, null);
        }

        public int CompareTo(VersionIdentifier? other)
        {
            if (other is null)
            {
                return 1;
            }

            if (NumericValue.HasValue && other.NumericValue.HasValue)
            {
                return NumericValue.Value.CompareTo(other.NumericValue.Value);
            }

            if (NumericValue.HasValue)
            {
                return -1;
            }

            if (other.NumericValue.HasValue)
            {
                return 1;
            }

            return string.CompareOrdinal(Value, other.Value);
        }
    }
}
