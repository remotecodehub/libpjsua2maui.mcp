# Documentation Guide

This directory is the project-level guidance for architecture, implementation, MCP design, package knowledge, testing, release, and roadmap decisions.

## Documents

- `architecture.md` — boundaries, responsibilities, data flow, hybrid hosting, and test project organization.
- `development-guidelines.md` — coding, testing, dependency, documentation, and review rules.
- `mcp-design.md` — intended MCP tools/resources and version-aware knowledge model.
- `package-knowledge.md` — how knowledge is extracted from the published NuGet package and related pjproject/SWIG sources.
- `testing.md` — test project organization and validation strategy for the shared core and both transports.
- `release.md` — NuGet/dotnet-tool and Docker/GHCR release expectations.
- `roadmap.md` — ordered implementation phases and completion criteria.

Agents should read the documents relevant to the task before modifying implementation. The roadmap is a planning document, not permission to skip validation or architectural constraints.