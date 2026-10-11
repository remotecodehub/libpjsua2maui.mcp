# Skill: MCP Transport Hosts

## Purpose

Use when implementing stdio or HTTP hosting.

## Shared rule

Hosts consume the same Core implementation. Do not implement business/query logic in `Program.cs` or transport-specific services.

## Stdio

Keep stdout exclusively for MCP protocol traffic. Send diagnostics to stderr. Ensure the packaged dotnet tool can run without network access when its knowledge set is bundled or locally indexed.

## HTTP

Host the MCP protocol over HTTP using the project's chosen MCP-compatible transport. Do not confuse arbitrary REST endpoints with an MCP transport. Keep ASP.NET-specific concerns in the HTTP project and make the Docker image reproducible and suitable for GHCR.

## Validation

Test initialization, discovery, request/response behavior, cancellation where supported, configuration, and startup independently for each host.