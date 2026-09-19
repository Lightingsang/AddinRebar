---
name: autocad-addin-review-hotspots
description: Recurring defect classes found when reviewing the HP AutoCAD add-ins (HPGeo 2026-09-18 x2, AEC engine rounds) — check these first on any AutoCAD reader/command/raster review
metadata:
  type: project
---

Check these first on any AutoCAD add-in review in this repo; each bit a shipped build at least once.

- Polyline vertices via `GetPoint2dAt` are OCS, not WCS: mirrored/tilted normals reflect X. The verified reference is `HPAutoCad.Aec/Cad/EntityShapeReader.cs` (`GetPoint3dAt` + `GetArcSegmentAt`). HPGeo `DrawingReader` had the bug at review time.
- Fixed chord count per arc (HPGeo used 8) is metres off at R ≥ 200 m; demand a sagitta-tolerance count (AEC `ArcSteps`).
- `CommandFlags.NoUndoMarker` on a command that writes anything (even an Xrecord) merges its change into the previous command's undo group and sets DBMOD; scripts then stall on "Save changes?". HPGeo HPGEO / -HPGEOKMZ did this.
- "Is it verified?" is answerable from disk: `HPGeo/output/acceptance/summary.json` check count + `%LocalAppData%\HPGeo\logs\hpgeo-YYYYMMDD.log` grep for the command name. HPGeo P3 (-HPGEOIMAGE) shipped with a harness block that had never run.
- Harness `U`-based checks: `U` consumes one command boundary even for non-undoable commands (it prints the name, does nothing), so `DELAY`/`ZOOM` between the command under test and `_.U` undo the wrong thing. Put `_.U` right after the command; take screenshots from a second run.
- A blocking wait on the main thread (`WaitOne` loop over a `Task.Run`) needs `HostApplicationServices.Current.UserBreak()` + a deadline; HPGeo imagery had 77 min worst case with Esc dead.
- Raster attach: `RasterImageDef.Load()` before `SetAt` has no database to resolve a relative `SourceFileName`; use `ActiveFileName` (settable) + relative `SourceFileName`, and `RasterImageDef.SuggestName(dict, path)` instead of adopting an existing key.
- Non-resident `DBObject`s (`new RasterImageDef/RasterImage/Polyline` that never reach `AddNewlyCreatedDBObject`) must be `Dispose()`d on the failure path — the finalizer deletes the native object off-thread and crashes acad.exe later.
- Optional-argument precedence (arg → DWG record → user): every parameter of the same group (cm + k0/fe/fn + unit) must follow one chain, and the write-back must not replace record values with parser defaults. Non-nullable "default applied in Parse" fields hide "absent".
- Reviews here get pinned by `*ReviewTests` files after the fix round (AEC phases A–I did this every time); ask for the same when handing back.

**Why:** these are not visible from the diff and none of the unit tests can exercise them (no acad.exe), so they survive every green test run.
**How to apply:** grep for `GetPoint2dAt`, `SegmentsPerArc|SegmentsPer`, `NoUndoMarker`, `WaitOne|GetResult`, `dict.Contains`, `new Raster` before reading anything else; verify parser/geometry claims with a scratch console against the Core project rather than reasoning from the code (probe caught 3 HPGeo findings in round 1 and the 372 m k0 shift in round 2); read AutoCAD.NET member claims from `~/.nuget/packages/autocad.net.model/25.1.0/lib/net8.0/AcDbMgd.dll` with `System.Reflection.Metadata` (PS 5.1 cannot load it).
