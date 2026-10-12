using System.Reflection;
using libpjsua2maui.mcp;
using libpjsua2maui.mcp.http;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks();
builder.Services.AddSingleton<IMcpHostHealthCheckService, HttpHostHealthCheckService>();

builder.Services
    .AddMcpServer()
    .WithHttpTransport(options =>
    {
        options.SessionMode = HttpServerSessionMode.Stateless;
    })
    .WithTools<HealthCheckTool>();

var app = builder.Build();

app.MapHealthChecks("/api/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";

        var response = new
        {
            status = report.Status.ToString(),
            totalDurationMs = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMs = entry.Value.Duration.TotalMilliseconds,
                error = entry.Value.Exception?.Message
            }),
            version = Assembly.GetExecutingAssembly().GetName().Version?.ToString()
        };

        await context.Response.WriteAsJsonAsync(response);
    }
});

app.MapMcp("/mcp");

app.Run();
