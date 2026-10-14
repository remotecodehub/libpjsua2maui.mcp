namespace libpjsua2maui.mcp.Knowledge;

/// <summary>Configures acquisition of a published package artifact.</summary>
public sealed record PackageAcquisitionOptions
{
    public PackageAcquisitionOptions(string storageDirectory, Uri? packageSource = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageDirectory);
        if (!Path.IsPathFullyQualified(storageDirectory))
        {
            throw new ArgumentException("The package storage directory must be absolute.", nameof(storageDirectory));
        }

        StorageDirectory = Path.GetFullPath(storageDirectory);
        PackageSource = packageSource ?? new Uri("https://api.nuget.org/v3-flatcontainer/", UriKind.Absolute);
        if (!PackageSource.IsAbsoluteUri || PackageSource.Scheme is not ("http" or "https"))
        {
            throw new ArgumentException("The package source must be an absolute HTTP(S) URI.", nameof(packageSource));
        }
    }

    public string StorageDirectory { get; }

    public Uri PackageSource { get; }
}
