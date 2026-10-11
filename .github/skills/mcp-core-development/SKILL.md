# Skill: MCP Core Development

## Purpose

Use this skill when implementing or changing shared MCP behavior.

## Workflow

1. Read `AGENTS.md` and `docs/architecture.md`.
2. Identify whether the change belongs to the transport-agnostic Core.
3. Keep package knowledge, indexing, models, and query behavior out of host projects.
4. Design tool/resource contracts around focused agent questions.
5. Preserve package-version and platform provenance in results.
6. Add tests before declaring the behavior complete.

## Rules

Never duplicate a tool between stdio and HTTP. Never make Core depend on ASP.NET. Never infer native mappings without evidence. Prefer deterministic structured results over prose assembled by the server.