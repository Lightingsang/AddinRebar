# 0005 — Static policy and mutable-state allowlist

- **Status:** Proposed (2026-10-03)
- **Tags:** [PCC] [REVIT] [PROJECT]

## Context
~104 files declare static classes in the add-in. Most are harmless (pure helpers, stateless Revit adapters). The harmful ones: mutable statics shared across windows (`PixelsPerDip` ×2, written from each canvas `OnRender`), a process-wide settings cache with no reset (`KataSettingsStore._cached`), and ViewModels calling static infrastructure (Excel COM, settings file) — which makes them untestable. Revit itself forces some statics (modeless window singleton, ExternalEvent ownership).

## Decision
1. **Allowed:** public static for pure, stable operations (PCC-171) — Core calculators, parsers, formatting; private static helpers (PCC-168); stateless Revit adapters that no caller needs to substitute (PCC-167).
2. **Allowlisted mutable statics:** `<Feature>Command._window`, Serilog `Log.Logger`, `RevitHostTheme.Instance`, `McpBridgeHost.Current`. Anything else needs a new ADR.
3. **Not allowed:** static access to infrastructure (Excel, files, clock, environment, dialogs) from ViewModels and orchestrators — wrap and inject (PCC-161, PCC-173, PCC-245); static render state shared between windows; new static caches without an injection/reset seam.
4. Converting stateless static adapters to instances is **not** a goal by itself; it happens when a caller needs a seam (PCC-123).

## Consequences
- \+ Removes the statics that cause real problems without churning ~100 classes.
- − Some orchestrators stay testable only in Revit until a seam is needed.

## Alternatives
"No statics" (rejected: contradicts PCC-171, large churn, no benefit for pure code). Status quo (rejected: VMs untestable, shared render state).

## Rules
PCC-167 … PCC-181, PCC-245, PCC-285; DEPENDENCY_RULES S1–S4.
