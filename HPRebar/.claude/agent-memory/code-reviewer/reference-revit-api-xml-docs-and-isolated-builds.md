---
name: reference-revit-api-xml-docs-and-isolated-builds
description: Where to verify Revit API exception contracts offline, and how to build HPRebar configs without touching the lead's obj/bin
metadata:
  type: reference
---

- Revit API XML docs (exceptions, "since" version) ship in the NuGet cache: `~/.nuget/packages/nice3point.revit.api.revitapi/<ver>/ref/<tfm>/RevitAPI.xml` (2023.1.90 net48, 2026.4.10 net8.0-windows7.0, 2027.2.0 net10). Grep `name="M:Autodesk.Revit.DB.Solid.IntersectWithCurve` etc. Use it instead of guessing which exception a member throws: e.g. `IntersectWithCurve` documents Autodesk `ArgumentException` for a non-closed solid, not `InvalidOperationException`.
- Compile-check a Revit config as a reviewer without colliding with a concurrent build: `dotnet build HPRebar/HPRebar.csproj -c Debug.R24 -p:DeployAddin=false --artifacts-path <scratchpad>/art24`. R23/R24 = net48 and catch R23+ API use (e.g. `Category.BuiltInCategory` since 2023).

- `dotnet test HPRebar.Core.Tests --artifacts-path <scratch>` fails the 3 `ThemeTokenCoverageTests` (they walk up from the test bin to `HPRebar.slnx`) — not a real failure. To run them isolated: copy the test bin under `<scratch>/fake/`, `touch <scratch>/fake/HPRebar.slnx`, `cmd //c mklink //J <scratch>\fake\HPRebar <repo>\HPRebar\HPRebar`, run `HPRebar.Core.Tests.exe --filter-class …ThemeTokenCoverageTests`; remove the junction with `cmd //c rmdir` (never `rm -rf` — it can follow the junction into the source).
- Theme-gallery screenshots of a run land where the lead pointed `-Out` (often the lead's scratchpad `p7/gallery/`), not `HPRebar/output/`; `find` Temp/claude for `*.png` to inspect them.

**Why:** a static review of Revit readers (no Revit available) hinges on exact API exception and version contracts.
**How to apply:** any review of HPRebar `.cs` touching Revit geometry or parameter APIs.
