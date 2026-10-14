namespace libpjsua2maui.mcp.Knowledge;

/// <summary>Acquires and validates one exact published package version.</summary>
public interface IPackageAcquirer
{
    /// <summary>Downloads or reuses a validated package from the configured source.</summary>
    Task<PackageAcquisitionResult> AcquireAsync(string version, CancellationToken cancellationToken = default);
}
