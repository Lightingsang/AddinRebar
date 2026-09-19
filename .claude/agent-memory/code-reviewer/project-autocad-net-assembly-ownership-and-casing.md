---
name: autocad-net-assembly-ownership-and-casing
description: AutoCAD.NET 25.1.0 — which DLL owns which namespace (Document/Editor in AcCoreMgd, Application/CloseAndDiscard in AcMgd) and the lowercase `accoremgd` identity that defeats case-sensitive prefix checks
metadata:
  type: project
---

AutoCAD.NET 25.1.0 (AutoCAD 2026) facts that reviews of `HPAutoCad/` keep needing and that are NOT in the repo:

- Assembly identities as referenced by compiled DLLs: `accoremgd` (lowercase!), `Acdbmgd`, `AcMgd`, `acdbmgdbrep`. A `StartsWith("Ac", Ordinal)` guard misses two of them.
- Namespace ownership (verified in `~/.nuget/packages/autocad.net*/25.1.0/lib/net8.0/*.xml`): `ApplicationServices.{Document,DocumentCollection,DocumentLock}`, `ApplicationServices.Core.Application` (incl. `Idle`, `IsQuiescent`, `Quit()`), `EditorInput.Editor` → **AcCoreMgd**. `ApplicationServices.Application` (the sample-code `Application.DocumentManager` idiom) and `DocumentExtension.CloseAndSave/CloseAndDiscard` → **AcMgd** (`CloseAndDiscard` exists in DLL metadata but is absent from the XML docs → `[undocumented]`). `Geometry`, `Colors`, `DatabaseServices` → **AcDbMgd**.
- So `typeof(Document).Assembly` is AcCoreMgd, not AcMgd; a compiler reference list built from `typeof(...)` needs `typeof(Autodesk.AutoCAD.ApplicationServices.Application).Assembly` to get AcMgd.

**Why:** phase-1 review (2026-09-14) found `BridgeEntry.CreateCompiler` referencing AcCoreMgd twice and AcMgd never, with a comment claiming otherwise; the seed compile tests in phase 3 will reference all three NuGet DLLs, so this is the drift the `HostScriptContracts` design exists to prevent.

**How to apply:** when reviewing any `ScriptCompiler` reference list, `ScriptGuard` deny-list, or ALC name filter in `HPAutoCad/`, check assembly identities with metadata (`System.Reflection.Metadata` over the DLL), not by reading comments. Review report: `plans/260913-0000-autocad-mcp-bridge-2026/reports/phase-01-code-review.md`.
