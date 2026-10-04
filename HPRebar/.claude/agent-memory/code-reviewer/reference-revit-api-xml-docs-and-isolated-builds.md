---
name: reference-revit-api-xml-docs-and-isolated-builds
description: Where to verify Revit API exception contracts offline, and how to build HPRebar configs without touching the lead's obj/bin
metadata:
  type: reference
---

- Revit API XML docs (exceptions, "since" version) ship in the NuGet cache: `~/.nuget/packages/nice3point.revit.api.revitapi/<ver>/ref/<tfm>/RevitAPI.xml` (2023.1.90 net48, 2026.4.10 net8.0-windows7.0, 2027.2.0 net10). Grep `name="M:Autodesk.Revit.DB.Solid.IntersectWithCurve` etc. Use it instead of guessing which exception a member throws: e.g. `IntersectWithCurve` documents Autodesk `ArgumentException` for a non-closed solid, not `InvalidOperationException`.
- Compile-check a Revit config as a reviewer without colliding with a concurrent build: `dotnet build HPRebar/HPRebar.csproj -c Debug.R24 -p:DeployAddin=false --artifacts-path <scratchpad>/art24`. R23/R24 = net48 and catch R23+ API use (e.g. `Category.BuiltInCategory` since 2023).

- R27 removed-API facts read from the 2026.4.10 XML (2026-10-03): `Curve.Intersect(Curve, CurveIntersectResultOption)` since 2026 — `GetOverlaps()` throws unless `Detailed`; points typed Intersection / IntervalStart+IntervalEnd (end-to-end = degenerate interval). `BarTerminationsData(doc)` default `TerminationOrientationAt*` = Left (old no-hook calls passed Right; inert without hooks). `REVIT2026_OR_GREATER` gates move R26 onto the new path too — say so when asked about "R23–R26 equivalence".
- `dotnet test HPRebar.Core.Tests --artifacts-path <scratch>` fails the 3 `ThemeTokenCoverageTests` (they walk up from the test bin to `HPRebar.slnx`) — not a real failure. To run them isolated: copy the test bin under `<scratch>/fake/`, `touch <scratch>/fake/HPRebar.slnx`, `cmd //c mklink //J <scratch>\fake\HPRebar <repo>\HPRebar\HPRebar`, run `HPRebar.Core.Tests.exe --filter-class …ThemeTokenCoverageTests`; remove the junction with `cmd //c rmdir` (never `rm -rf` — it can follow the junction into the source).
- Old-vs-new differential probe for a Core calculator fix: scratch net8 console with `<Reference HintPath=".../HPRebar.Core.Tests/bin/Debug/net8.0/HPRebar.Core.dll">`, old formula pasted inline, 1M random + special inputs; `dotnet run -c Release` takes ~10 s.
- Theme-gallery screenshots of a run land where the lead pointed `-Out` (often the lead's scratchpad `p7/gallery/`), not `HPRebar/output/`; `find` Temp/claude for `*.png` to inspect them.

**Why:** a static review of Revit readers (no Revit available) hinges on exact API exception and version contracts.
**How to apply:** any review of HPRebar `.cs` touching Revit geometry or parameter APIs.
