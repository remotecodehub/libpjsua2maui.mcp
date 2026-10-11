# Release Guide

## Distribution model

The project has two intended distributions backed by the same shared implementation:

1. **dotnet tool / stdio** — NuGet-packaged executable for local agents and offline usage.
2. **Docker / GHCR** — ASP.NET HTTP MCP host distributed as an immutable container image.

The shared library may be published as a normal NuGet package if useful, but consumers should not need to reference it directly merely to use either host.

## Versioning

Keep application/MCP versioning independent from the indexed `libpjsua2maui` package version. The MCP must expose both versions so an agent can distinguish server capability from package knowledge.

Prefer immutable container tags for releases and maintain a clear mapping between image tag, MCP version, and indexed package version.

## Container requirements

The HTTP image must be minimal, reproducible, non-root where practical, expose only the required MCP endpoint, and contain the package knowledge needed for normal operation. Do not include platform-specific native pjproject binaries unless a future feature explicitly requires them and the image architecture supports them.

## dotnet-tool requirements

The stdio executable must not write diagnostic logs to stdout because stdout is the MCP transport. Diagnostics belong on stderr or an appropriate configured sink. Offline mode must operate using bundled or locally indexed knowledge.

## CI/CD expectations

Before publishing, CI should restore, build, test, validate packaging, build the container, and verify that the produced artifacts contain the expected host and knowledge data. Release automation must not publish an unverified package or image.