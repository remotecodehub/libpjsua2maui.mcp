using System.Reflection;

namespace libpjsua2maui.mcp.tests.Stdio.TestSupport.Helpers;

/// <summary>
/// Resolves the stdio executable built with the current test configuration.
/// </summary>
internal static class StdioHostPaths
{
    public static string DotnetHostPath =>
        Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet";

    public static string AssemblyPath => GetAssemblyPath(
        "src/libpjsua2maui.mcp.stdio",
        "libpjsua2maui.mcp.stdio.dll");

    public static string RepositoryRoot
    {
        get
        {
            var metadata = typeof(StdioHostPaths).Assembly
                .GetCustomAttributes<AssemblyMetadataAttribute>()
                .Single(attribute => attribute.Key == "RepositoryRoot");

            return Path.GetFullPath(metadata.Value
                ?? throw new InvalidOperationException("RepositoryRoot metadata is empty."));
        }
    }

    private static string GetAssemblyPath(string projectPath, string assemblyName)
    {
        var targetFrameworkDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        var configuration = targetFrameworkDirectory.Parent?.Name
            ?? throw new InvalidOperationException("Unable to determine the test build configuration.");

        var assemblyPath = Path.Combine(
            RepositoryRoot,
            projectPath,
            "bin",
            configuration,
            "net10.0",
            assemblyName);

        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException(
                $"Expected host assembly at '{assemblyPath}'. Build the solution before running process tests.",
                assemblyPath);
        }

        return assemblyPath;
    }
}
