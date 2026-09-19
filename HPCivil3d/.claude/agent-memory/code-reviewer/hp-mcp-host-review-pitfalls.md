---
name: hp-mcp-host-review-pitfalls
description: Recurring review traps across the HP MCP hosts (AutoCAD/Civil 3D bridges + harnesses) — Debug is the deployed config, SECURELOAD auto-answer is global, mirror-token drift markers
metadata:
  type: project
---

Facts confirmed while reviewing Civil 3D phase 1 (2026-09-18) that a diff alone does not show:

- **Debug IS the deployed configuration for the acad.exe-family bundles.** `DeployBundle` runs only for `-c Debug`, so `#if DEBUG`-gated code (e.g. the Civil spike guard bypass `HPCIVIL3D_MCP_SPIKE`) ships in the bundle the user actually runs. Treat "Debug only" as no protection; the real gate is whatever the code checks at run time.
- **`Answer-SecureLoad` in both `HPAutoCad/tools/harness/harness-common.ps1` and the Civil copy is global** (`FindWindow` by title, not pid- or file-scoped) and clicks "Always Load" = permanent trust. Flag whenever a harness runs beside the user's foreign bundles (`Civil3dMcp.bundle`, `AutoCadMcp.bundle` load into every product).
- **Civil copy vs AutoCAD source:** `HPCivil3d/tools/mirror-tokens.json` is an ordered replace table; the promised phase-2 mirror test compares outside `// civil-only: begin/end` blocks, but as of phase 1 only 3 of 8 drifted files carry the markers. Re-check marker coverage when the mirror test lands.
- Per-run env cleanup: harness `finally` blocks tend to remove only the spike var and leave `*_MCP_Registry__*` / `HP_HARNESS_ACAD_PID` in the caller's shell.

**Why:** these were the Medium findings of `plans/260917-1633-civil3d-mcp-2026/reports/code-review-phase-01.md`; they recur because every new host starts as a copy of the AutoCAD folder.
**How to apply:** when reviewing any `HP<Host>/` bridge or `tools/harness/*.ps1`, check these four first before reading the diff.
