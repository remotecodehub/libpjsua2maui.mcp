using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using libpjsua2maui.mcp.stdio;
using libpjsua2maui.mcp.stdio.Commands;

if (args.Length > 0)
{
    // CLI commands are separate from the MCP stdio protocol and may write to stdout.
    var commandServices = new ServiceCollection();
    commandServices.AddSingleton<StdioHealthCheckService>();
    commandServices.AddSingleton<HealthCheckCommand>();

    using var commandProvider = commandServices.BuildServiceProvider();

    var rootCommand = new RootCommand("Local stdio MCP server for libpjsua2maui.");
    rootCommand.Subcommands.Add(commandProvider.GetRequiredService<HealthCheckCommand>().CreateCommand());

    return await rootCommand.Parse(args).InvokeAsync();
}

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    // MCP stdio reserves stdout for protocol messages; logs must go to stderr.
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});

builder.Services.AddSingleton<StdioHealthCheckService>();
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

using var host = builder.Build();

await host.RunAsync();

return 0;
