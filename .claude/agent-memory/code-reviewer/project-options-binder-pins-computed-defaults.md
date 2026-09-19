---
name: options-binder-pins-computed-defaults
description: In the MCP server options classes, a "computed default" getter (`_x ?? Derive(Other)`) gets written back and pinned by Microsoft.Extensions.Configuration.Binder whenever the bound section has ≥1 key — PostConfigure of the base property then has no effect
metadata:
  type: project
---

Pattern to distrust in `HPRebar.Mcp.Server.Core` options (`RegistryOptions.LibraryPath/DbPath`, `BridgeOptions.PipeName`): a settable property whose getter derives from another property (`ProductFolder`, `HostId`), with the "other" property set later via `PostConfigure` in `McpServerHost.CreateBuilder`.

**Why:** `Bind()` reads every settable property's current value and writes it back when the section is non-empty (binder 10.0.12, verified empirically 2026-09-14 with a scratch console: AutoCAD profile + any `Registry:*`/`Bridge:*` key → `LibraryPath` pinned to the HPRebar root, `PipeName` pinned to `hprebar-mcp-r2026`). With an empty section the binder skips entirely, so a unit test without config passes. The shipped `appsettings.json` always has keys in both sections.

**How to apply:** when reviewing any `AddOptions<T>().Bind(...).PostConfigure(...)` in this repo, ask whether a getter in `T` derives from a property that `PostConfigure` changes; if yes, require `Configure` *before* `Bind` or plain nullable properties resolved in `PostConfigure`, plus a test that binds a section with one unrelated key. Do not accept "the unit test passes" as evidence.
