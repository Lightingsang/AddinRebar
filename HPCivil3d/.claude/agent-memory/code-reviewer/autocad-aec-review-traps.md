---
name: autocad-aec-review-traps
description: AutoCAD .NET / HPAutoCad.Aec review traps — Table derives BlockReference, how to probe pure Aec code and AcDbMgd metadata off-AutoCAD
metadata:
  type: project
---

- `Autodesk.AutoCAD.DatabaseServices.Table : BlockReference` (also `MInsertBlock`); `Dimension`, `MLeader` derive `Entity`. A scanner
  that tests `is BlockReference` before `Table` never sees tables (found in the VN-text scanner 2026-10-14).
- Pure Aec classes (Text/, Geometry/…) can be probed without AutoCAD: a scratch net10.0-windows console referencing
  `HPAutoCad.Aec.Tests/bin/Debug/net10.0-windows/HPAutoCad.Aec.dll` (+ deps) runs them; AcDbMgd inheritance is read with
  System.Reflection.Metadata from `~/.nuget/packages/autocad.net.model/25.1.0/lib/net8.0/AcDbMgd.dll` (loading it as a type fails).
- Hand-written MText walkers: check the trailing-`\` case — a branch that does not advance spins forever on acad's main thread,
  and `ct` is usually checked per entity only.
- Style/font remaps: check that every text still in legacy bytes under that style was converted (filters, atomic:false, skipped
  kinds) — remapping first is the garbage-on-screen bug.

**Why:** these were the Critical/High findings of `plans/261014-1339-vnconvert-vn-text-encoding-autocad/reports/code-review-vnconvert.md`.
**How to apply:** when reviewing any HPAutoCad.Aec entity walker or text writer, check the type-switch order and probe pure code.
