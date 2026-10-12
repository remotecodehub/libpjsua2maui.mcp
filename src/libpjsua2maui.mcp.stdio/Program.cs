using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using libpjsua2maui.mcp.stdio.Commands;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    // MCP stdio reserves stdout for protocol messages; logs must go to stderr.
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

builder.Services.AddSingleton<StdioHealthCheckService>();
builder.Services.AddSingleton<HealthCheckCommand>();

using var host = builder.Build();

if (args.Length > 0)
{
    var rootCommand = new RootCommand("Local stdio MCP server for libpjsua2maui.");
    rootCommand.Subcommands.Add(host.Services.GetRequiredService<HealthCheckCommand>().CreateCommand());

    return await rootCommand.Parse(args).InvokeAsync();
}

await host.RunAsync();

return 0;
