# AGENTS.md

## Purpose
`libpjsua2maui.mcp` provides a dual-host MCP for agents developing .NET MAUI applications with the `libpjsua2maui` NuGet package. The shared class library owns MCP/domain behavior; the stdio host targets local/offline agents and the HTTP host targets containerized/shared agents.

## Mandatory first steps
Before changing code, read:

1. `docs/README.md`
2. `docs/architecture.md`
3. `docs/development-guidelines.md`
4. `docs/roadmap.md`
5. Relevant `.github/skills/*/SKILL.md`

Also inspect the current solution and existing project files before introducing new abstractions.

## Source of truth
The MCP is a knowledge/query service, not a runtime replacement for pjproject. Its knowledge must be version-aware and should prefer the actually published `libpjsua2maui.nupkg`, generated C# SWIG bindings, XML documentation when available, and repository metadata over assumptions. Preserve provenance from C# symbol to SWIG/native pjproject mapping whenever that information can be established.

`libpjsua2maui` wraps native pjproject per MAUI target. Do not assume that a managed type implies identical native availability on Android, iOS, MacCatalyst, and Windows.

## Architecture rules
- Keep the shared MCP implementation in `src/libpjsua2maui.mcp`.
- Keep transport/hosting concerns in `src/libpjsua2maui.mcp.stdio` and `src/libpjsua2maui.mcp.http`.
- Keep all tests in `tests/libpjsua2maui.mcp.tests`.
- Organize tests into `MCP`, `Http`, and `Stdio` top-level folders matching the production boundaries.
- Keep fixtures, mocks, fakes, stubs, and helpers under the `TestSupport` folder of the test area they serve.
- Do not duplicate MCP tools, resources, indexing, package inspection, or domain rules between hosts.
- Keep the core transport-agnostic and testable.
- Treat MCP protocol handling and ordinary REST concerns as transport/host concerns; do not turn the knowledge core into an ASP.NET dependency.
- Version all package-derived knowledge. Never silently mix symbols from different package versions.
- Prefer deterministic, structured results with source/provenance metadata.
- Production projects must never reference the test project.

## C# implementation rules
- Target the repository's configured .NET version and use modern .NET APIs and analyzers.
- Prefer primary constructors where they improve clarity and do not obscure invariants.
- Use nullable reference types and explicit null handling.
- Prefer async/await for I/O-bound operations; propagate `CancellationToken`.
- Prefer immutable/read-only models for query results and indexed metadata.
- Avoid single-line method implementations. Expression-bodied members are allowed when the expression is genuinely clearer and remains readable.
- Use one top-level type per `.cs` file: class, record, struct, enum, interface, delegate, or other named type.
- Exception: a common generic implementation may live in the same file as its generic specialization when the specialization is intentionally a direct companion and both belong to the same cohesive abstraction.
- Prefer collection expressions and modern pattern matching where they improve readability.
- Keep methods small, cohesive, and explicit about side effects.
- Do not introduce service locators, static mutable state, or hidden global caches.
- Use XML documentation for public APIs whose intent is not obvious from the signature.
- Avoid unnecessary comments; comments should explain decisions, constraints, native/platform behavior, or non-obvious protocol details.

## Testing and validation
Every implementation stage must build the solution and run relevant tests. Add unit tests for core behavior before relying on host-level tests. Validate both hosts when transport code changes. Keep test fixtures and doubles isolated by test area. Do not claim support for a platform, package version, MCP feature, or native symbol without evidence.

**Mandatory test quality gate:** From the repository root, run `dotnet tool restore` before invoking repository-local tools. Then run exactly:

```powershell
dotnet build ./tests/libpjsua2maui.mcp.tests/libpjsua2maui.mcp.tests.csproj -c Release -t:QualityGate
```

The custom `QualityGate` target runs tests, generates reports, and enforces at least 90% line, branch, and method coverage. A warning is emitted for each dimension below threshold and the target fails if any dimension is below 90%. A successful build or plain test run is not a substitute. Report the actual coverage percentages from the generated report and claim success only if the command exits successfully and all three dimensions meet the threshold.

If the command is blocked by environment/tooling limitations, stop and notify the user with the exact command and error; do not silently bypass the gate, lower its threshold, or remove the existing target. Wait for the user to adjust the target and authorize resumption, unless an additional compatible target that preserves the existing one is clearly the appropriate fix.

## Git discipline
Keep changes focused. Do not rewrite unrelated project files. Update documentation and roadmap status when architecture or capabilities change. Use clear conventional commit messages. Changes to `main` are allowed for repository bootstrap/documentation work when explicitly requested; implementation work should normally be reviewed through the repository's established workflow.
