# Skill: libpjsua2maui Package Analysis

## Purpose

Use when extracting or validating knowledge from `libpjsua2maui`.

## Workflow

- Treat the published NuGet package as the consumer contract.
- Record exact package version and target framework/platform.
- Inspect managed assembly metadata and XML documentation first.
- Inspect generated SWIG artifacts when available.
- Use pjproject source/header information only to enrich a demonstrable mapping.
- Record provenance and confidence for enriched facts.

## Native boundary

Remember that the managed API is a binding over native pjproject compiled for MAUI targets. Distinguish managed compile-time availability from native runtime availability. Do not execute platform-native binaries as part of ordinary package indexing.