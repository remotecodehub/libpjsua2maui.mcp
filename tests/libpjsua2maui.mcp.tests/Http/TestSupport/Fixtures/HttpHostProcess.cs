using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;

namespace libpjsua2maui.mcp.tests.Http.TestSupport.Fixtures;

/// <summary>
/// Starts the actual HTTP host on a dynamically allocated loopback port for integration tests.
/// </summary>
internal sealed class HttpHostProcess : IAsyncDisposable
{
    private readonly Process _process;
    private readonly Task<string> _standardOutputTask;
    private readonly Task<string> _standardErrorTask;
    private readonly HttpClient _httpClient;

    private HttpHostProcess(Process process, Uri baseAddress)
    {
        _process = process;
        _standardOutputTask = process.StandardOutput.ReadToEndAsync();
        _standardErrorTask = process.StandardError.ReadToEndAsync();
        BaseAddress = baseAddress;
        _httpClient = new HttpClient
        {
            BaseAddress = baseAddress,
            Timeout = TimeSpan.FromSeconds(3)
        };
    }

    public Uri BaseAddress { get; }

    public HttpClient HttpClient => _httpClient;

    public Uri McpEndpoint => new(BaseAddress, "mcp");

    public static async Task<HttpHostProcess> StartAsync()
    {
        var port = GetAvailablePort();
        var baseAddress = new Uri($"http://127.0.0.1:{port}/");
        var assemblyPath = GetHttpAssemblyPath();

        var startInfo = new ProcessStartInfo
        {
            FileName = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH") ?? "dotnet",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add(assemblyPath);
        startInfo.ArgumentList.Add("--urls");
        startInfo.ArgumentList.Add(baseAddress.ToString());

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start the HTTP host process.");

        var host = new HttpHostProcess(process, baseAddress);

        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            while (!timeout.IsCancellationRequested)
            {
                if (process.HasExited)
                {
                    throw await host.CreateStartupExceptionAsync();
                }

                try
                {
                    using var response = await host.HttpClient.GetAsync("api/health", timeout.Token);
                    if (response.IsSuccessStatusCode)
                    {
                        return host;
                    }
                }
                catch (HttpRequestException)
                {
                    // Kestrel may not be listening yet; retry until the startup deadline.
                }
                catch (TaskCanceledException) when (!timeout.IsCancellationRequested)
                {
                    // The short HTTP request timeout elapsed while the server was starting.
                }

                await Task.Delay(TimeSpan.FromMilliseconds(150), timeout.Token);
            }

            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
            }

            throw await host.CreateStartupExceptionAsync();
        }
        catch
        {
            await host.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _httpClient.Dispose();

        if (!_process.HasExited)
        {
            _process.Kill(entireProcessTree: true);
            await _process.WaitForExitAsync();
        }

        await Task.WhenAll(_standardOutputTask, _standardErrorTask);
        _process.Dispose();
    }

    private async Task<InvalidOperationException> CreateStartupExceptionAsync()
    {
        var standardOutput = await _standardOutputTask;
        var standardError = await _standardErrorTask;

        return new InvalidOperationException(
            $"The HTTP host failed to start or become healthy. stdout: {standardOutput} stderr: {standardError}");
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();

        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private static string GetHttpAssemblyPath()
    {
        var metadata = typeof(HttpHostProcess).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "RepositoryRoot");

        var repositoryRoot = Path.GetFullPath(metadata.Value
            ?? throw new InvalidOperationException("RepositoryRoot metadata is empty."));
        var targetFrameworkDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        var configuration = targetFrameworkDirectory.Parent?.Name
            ?? throw new InvalidOperationException("Unable to determine the test build configuration.");

        var assemblyPath = Path.Combine(
            repositoryRoot,
            "src",
            "libpjsua2maui.mcp.http",
            "bin",
            configuration,
            "net10.0",
            "libpjsua2maui.mcp.http.dll");

        if (!File.Exists(assemblyPath))
        {
            throw new FileNotFoundException(
                $"Expected host assembly at '{assemblyPath}'. Build the solution before running HTTP integration tests.",
                assemblyPath);
        }

        return assemblyPath;
    }
}
