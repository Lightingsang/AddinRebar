# 0003 — Command as composition root, constructor injection, no DI container

- **Status:** Accepted (2026-10-03, by user)
- **Tags:** [PCC] [PROJECT]

## Context
No DI container is wired (verified: 0 `IServiceCollection` / `ILogger<T>` in the add-in). Each `<Feature>Command.Execute` already builds the object graph with `new`. Below that, ~104 static classes are called directly, so orchestrators and ViewModels have no seams. docs/code-standards.md §4 still says "constructor inject `ILogger<T>` + services", which the code never did. The MCP server does use Microsoft.Extensions.Hosting DI — inside `McpShared`, in another process.

## Decision
1. `<Feature>Command.Execute` is the **only** composition root of a feature ("pure DI").
2. Collaborators that vary or touch infrastructure are passed through the **constructor** (PCC-157, PCC-180, PCC-244); constructors stay trivial (PCC-289).
3. **No DI container** in the add-in. Revisit only when a graph is shared across features or grows beyond what one Command wires readably.
4. Logging stays on static Serilog `Log` (allowlisted); `ILogger<T>` is not introduced.
5. The `ILogger<T>` line of docs/code-standards.md §4 is superseded by this ADR.

## Consequences
- \+ Dependencies visible on constructors; no container lifetime bugs inside Revit's process; zero new packages.
- − Commands get a few more lines of wiring — acceptable, wiring is the Command's job.

## Alternatives
Microsoft.Extensions.DependencyInjection per add-in (rejected: extra assembly in Revit's load context, indirection for 5 short graphs — PCC-236/237). Service locator (rejected: hidden dependencies — PCC-175).

## Rules
PCC-157, PCC-158, PCC-160, PCC-175, PCC-180, PCC-244, PCC-289.
