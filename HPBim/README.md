# HPBim: Autonomous C# .NET BIM Model Generator for Revit 2026

HPBim is an independent, high-level C# .NET deliverable that automates the generation of native 3D BIM models (Architecture, Structure, MEP) in Autodesk Revit 2026 via the existing Revit MCP Named Pipe infrastructure (`\\.\pipe\hprebar-mcp-r2026`).

## Architecture & Layers

- **`HPBim.Core`** (`netstandard2.0`): Pure geometric primitives, domain BIM schemas, TCVN 5574:2018 detailing rules, and offline pre-flight QC clash detection. Zero Revit references.
- **`HPBim.Client`** (`net8.0;net10.0`): Named pipe communication client, 8-stage BIM creation pipeline, dynamic family/type synthesis, and non-destructive `dryRun` transaction rollback.
- **`HPBim.Cli`** (`net10.0`): Console executable providing `validate`, `generate`, `verify`, and `estimate` commands.
- **`HPBim/agents/`**: Packaged Vietnamese construction AI agent personas.

## Standards Compliance

- Strictly adheres to Clean Code Governance (`docs/clean-code/HP_CLEAN_CODE_CORE.md`).
- File-scoped namespaces, nullable reference types enabled, warnings treated as errors.
- Fast, deterministic unit tests run in seconds without launching Revit.
