# Skill: .NET Quality and Validation

## Purpose

Use for every C# implementation task.

## Rules

Follow `AGENTS.md` and `docs/development-guidelines.md`. Prefer primary constructors, nullable reference types, async APIs with cancellation, modern collection expressions/pattern matching, and clear block-bodied methods. Avoid compressed single-line methods except genuinely clear expression-bodied members. Keep one named type per file, with the documented generic companion exception.

## Validation

Build the affected project and solution when practical. Run focused tests first, then the complete relevant test suite. Check analyzer/compiler warnings introduced by the change. Update documentation and roadmap state when behavior changes.