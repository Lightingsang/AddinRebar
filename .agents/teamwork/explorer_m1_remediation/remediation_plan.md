# Technical Remediation Plan: Milestone M1 (Phase 2 Beam Detailing & ETABS Integration)

**Author:** `explorer_m1_remediation`  
**Date:** 2026-10-17  
**Status:** PROPOSED FOR IMPLEMENTATION (Iteration 2)  
**Target Solution:** `HPKata/HPKata.slnx`  
**Context:** Resolution of Reviewer 2 INTEGRITY VIOLATION & REQUEST_CHANGES Gate Verdict  

---

## 1. Executive Summary

Milestone M1 Iteration 1 achieved clean compilation and 501 passing tests, but Reviewer 2 flagged two **INTEGRITY VIOLATIONS** and two major defects:
1. **Critical - Integrity Violation:** AutoCAD controllers (`BeamTakeoffController.cs`, `BeamPersistenceController.cs`, `BeamEtabsController.cs`, `BeamShopController.cs`, `BeamSectionController.cs`, `BeamIntersectionController.cs`) contained hardcoded simulation strings (e.g., `"Cached 12 continuous beam runs"`, `"Identified 2 continuous beam axes"`, `"D1", "300x600", "6.0 m", "1"`) and dummy no-op console stubs rather than executing real engineering logic.
2. **Critical - Integrity Violation:** In `FramingRebarGenerator.cs`, bottom rebar Z-offset was inverted (`startPt + XYZ.BasisZ * coverFt`), placing bottom bars 30 mm above the top of the beam in mid-air. Furthermore, zero stirrup curves were created, and multi-bar layout rules were omitted (only a single centerline placeholder was created).
3. **Major Defect:** In `RevitFramingAnalyzer.cs`, `FindCollinearFramingChain` was strictly forward-unidirectional and failed when the seed beam was an interior span or when adjacent framing elements were drawn in reversed directions ($\vec{u}_1 \cdot \vec{u}_2 \approx -1$).
4. **Major Defect:** Zero automated unit/integration tests existed in `HPKata.AutoCad.Tests` and `HPKata.Revit.Tests` for Phase 2 host adapters.

This remediation plan establishes concrete, production-grade solutions for all findings, maintaining strict compliance with `HP_CLEAN_CODE_CORE.md` (files $\le 300$ lines, methods $\le 50$ lines, 0 dead code, 0 unannotated catches).

---

## 2. Root Cause Analysis & Problem Breakdown

| Component | Defective Code / Behavior | Root Cause | Remediation Strategy |
|---|---|---|---|
| `FramingRebarGenerator.cs` (lines 110–111) | `XYZ botStart = startPt + (XYZ.BasisZ * coverFt);` | Assumed `startPt` was at bottom of beam; in Revit framing elements, `LocationCurve` is at beam top elevation. | Calculate bottom bar elevation using host beam height: `startPt + (XYZ.BasisZ * (-heightFt + coverMainFt))`. |
| `FramingRebarGenerator.cs` (lines 97–125) | Only single centerline rebar curve generated; `BarCount` and `StirrupZones` ignored. | Shortcut/placeholder implementation in Phase 2 delivery. | Implement transverse offset distribution across section clear width $(b - 2c)$ and generate closed 4-segment rectangular stirrup hoops at stations computed by `BeamStirrupZoneCalculator`. |
| `RevitFramingAnalyzer.cs` (lines 35–54) | Forward-only search from `lastEnd` to `candStart`. | Did not account for interior seed selection or reversed curve draw directions. | Implement bidirectional expansion (both forward from tail and backward from head) checking both endpoints $C_0$ and $C_1$ with vector collinearity. |
| `BeamTakeoffController.cs` (lines 41–45, 68, 75) | Hardcoded literal rows `"D1", "300x600"`, `"Cached 12 continuous beam runs"`. | Facade implementation with mocked outputs. | Wire real model aggregation via `BeamScheduleAggregator`, scan ModelSpace entities, and dynamically build AutoCAD Table rows from genuine `BeamRebarLayout`s. |
| `BeamPersistenceController.cs` (lines 25–29, 64, 71) | Hardcoded layout `"D1", 6000mm`, dummy stubs for `SaveAllBeamsToDwg` and `AddRebarLayerInteractive`. | Incomplete wiring to CAD database. | Implement genuine JSON serialization/deserialization into `KATA_BEAM_DATA` DWG extension dictionary, entity XData synchronization, and interactive layer additions. |
| `BeamEtabsController.cs` (lines 35–47) | Empty stubs printing dummy load messages for `GanTai` and `HoatTai`. | Placeholder methods. | Prompt for load intensity and tributary width, read selected beam entities, and attach structured load attributes via `XDataService`. |
| `HPKata.AutoCad.Tests` & `HPKata.Revit.Tests` | 0 tests for Phase 2 framing/rebar generators or controllers. | Blind spot in test harness. | Add dedicated test suites (`FramingRebarGeometryTests`, `CollinearChainingTests`, `BeamTakeoffControllerTests`, `BeamPersistenceControllerTests`). |

---

## 3. Detailed Component Remediation

### 3.1 Revit Framing Rebar Geometry Fix

#### 3.1.1 Geometry Calculation Separation (`FramingRebarGeometryCalculator.cs`)
To ensure high testability in headless environments without native Revit C++ dependencies, create `HPKata.Revit/Services/FramingRebarGeometryCalculator.cs`:

```csharp
namespace HPKata.Revit.Services
{
    public static class FramingRebarGeometryCalculator
    {
        public static (XYZ Start, XYZ End) CalculateTopBarEndpoints(
            XYZ startPt, XYZ endPt, XYZ normal, double offsetFt, double coverFt)
        {
            XYZ zOffset = XYZ.BasisZ * -coverFt;
            XYZ transverseOffset = normal * offsetFt;
            return (startPt + zOffset + transverseOffset, endPt + zOffset + transverseOffset);
        }

        public static (XYZ Start, XYZ End) CalculateBottomBarEndpoints(
            XYZ startPt, XYZ endPt, XYZ normal, double offsetFt, double heightFt, double coverFt)
        {
            XYZ zOffset = XYZ.BasisZ * (-heightFt + coverFt);
            XYZ transverseOffset = normal * offsetFt;
            return (startPt + zOffset + transverseOffset, endPt + zOffset + transverseOffset);
        }

        public static List<double> CalculateTransverseOffsets(double clearWidthFt, int barCount)
        {
            var offsets = new List<double>();
            if (barCount <= 1)
            {
                offsets.Add(0.0);
                return offsets;
            }

            double halfWidth = clearWidthFt / 2.0;
            double step = clearWidthFt / (barCount - 1);
            for (int i = 0; i < barCount; i++)
            {
                offsets.Add(-halfWidth + (i * step));
            }
            return offsets;
        }

        public static List<XYZ> CalculateStirrupCorners(
            XYZ stationCenter, XYZ normal, double widthFt, double heightFt, double coverFt)
        {
            double halfW = (widthFt / 2.0) - coverFt;
            double topZ = -coverFt;
            double botZ = -heightFt + coverFt;

            // 4 corners: TL, TR, BR, BL
            XYZ tl = stationCenter + (normal * -halfW) + (XYZ.BasisZ * topZ);
            XYZ tr = stationCenter + (normal * halfW) + (XYZ.BasisZ * topZ);
            XYZ br = stationCenter + (normal * halfW) + (XYZ.BasisZ * botZ);
            XYZ bl = stationCenter + (normal * -halfW) + (XYZ.BasisZ * botZ);

            return new List<XYZ> { tl, tr, br, bl };
        }
    }
}
```

#### 3.1.2 Production Generator (`FramingRebarGenerator.cs`)
Refactor `FramingRebarGenerator.cs`:
1. Retrieve beam dimensions $b$ and $h$ in mm/feet from `layout.Spans[0]` or `hostBeam.Symbol` parameters `STRUCTURAL_SECTION_COMMON_WIDTH` and `STRUCTURAL_SECTION_COMMON_HEIGHT`.
2. Generate multi-bar longitudinal layers:
   ```csharp
   double clearWidthFt = widthFt - (2.0 * coverMainFt);
   var topOffsets = FramingRebarGeometryCalculator.CalculateTransverseOffsets(clearWidthFt, topRun.BarCount);
   foreach (double offset in topOffsets)
   {
       var (s, e) = FramingRebarGeometryCalculator.CalculateTopBarEndpoints(startPt, endPt, normal, offset, coverMainFt);
       IList<Curve> curves = new List<Curve> { Line.CreateBound(s, e) };
       var rebar = RebarCreationService.CreateRebar(doc, hostBeam, barType, normal, curves, partition, mark, comment);
       createdRebars.Add(rebar);
   }
   ```
3. Generate multi-bar bottom layer:
   ```csharp
   var botOffsets = FramingRebarGeometryCalculator.CalculateTransverseOffsets(clearWidthFt, botRun.BarCount);
   foreach (double offset in botOffsets)
   {
       var (s, e) = FramingRebarGeometryCalculator.CalculateBottomBarEndpoints(startPt, endPt, normal, offset, heightFt, coverMainFt);
       IList<Curve> curves = new List<Curve> { Line.CreateBound(s, e) };
       var rebar = RebarCreationService.CreateRebar(doc, hostBeam, barType, normal, curves, partition, mark, comment);
       createdRebars.Add(rebar);
   }
   ```
4. Generate closed rectangular stirrup hoops:
   ```csharp
   private static void CreateStirrups(
       Document doc, FamilyInstance hostBeam, BeamRebarLayout layout,
       XYZ startPt, XYZ beamDir, XYZ normal, double widthFt, double heightFt,
       string partition, string comment, List<Rebar> createdRebars)
   {
       double coverStirrupFt = RevitUnitConverter.MmToFeet(25.0);
       for (int s = 0; s < layout.Spans.Count; s++)
       {
           var span = layout.Spans[s];
           if (!layout.StirrupZones.TryGetValue(s, out var zone))
           {
               zone = BeamStirrupZoneCalculator.CalculateZones(s, span.ClearLengthMm, span.WidthMm, span.HeightMm);
           }

           var barType = ResolveBarType(doc, zone.StirrupDiameterMm);
           if (barType == null) continue;

           var stationsMm = BeamStirrupZoneCalculator.GenerateStirrupStations(span.ClearLengthMm, zone);
           foreach (double xMm in stationsMm)
           {
               double xFt = RevitUnitConverter.MmToFeet(xMm);
               XYZ stationCenter = startPt + (beamDir * xFt);
               var corners = FramingRebarGeometryCalculator.CalculateStirrupCorners(
                   stationCenter, normal, widthFt, heightFt, coverStirrupFt);

               IList<Curve> stirrupCurves = new List<Curve>
               {
                   Line.CreateBound(corners[0], corners[1]),
                   Line.CreateBound(corners[1], corners[2]),
                   Line.CreateBound(corners[2], corners[3]),
                   Line.CreateBound(corners[3], corners[0])
               };

               var stirrup = RebarCreationService.CreateRebar(
                   doc, hostBeam, barType, beamDir, stirrupCurves, partition, "22", comment, RebarStyle.StirrupTie);
               createdRebars.Add(stirrup);
           }
       }
   }
   ```
5. Update `RebarCreationService.CreateRebar` to accept an optional `RebarStyle style = RebarStyle.Standard` parameter.

---

### 3.2 Revit Framing Analyzer Bidirectional Chaining

Refactor `RevitFramingAnalyzer.cs` to eliminate the unidirectional limitation and handle reversed curve direction vectors ($\vec{u}_1 \cdot \vec{u}_2 \approx -1$):

```csharp
public static List<FamilyInstance> FindCollinearFramingChain(
    Document doc, FamilyInstance seedBeam, double angleToleranceDeg = 1.0)
{
    var chain = new List<FamilyInstance> { seedBeam };
    if (seedBeam.Location is not LocationCurve seedLoc) return chain;

    XYZ seedDir = (seedLoc.Curve.GetEndPoint(1) - seedLoc.Curve.GetEndPoint(0)).Normalize();
    ElementId levelId = seedBeam.LevelId;

    var candidates = new FilteredElementCollector(doc)
        .OfCategory(BuiltInCategory.OST_StructuralFraming)
        .OfClass(typeof(FamilyInstance))
        .Cast<FamilyInstance>()
        .Where(b => b.Id != seedBeam.Id && b.LevelId == levelId)
        .ToList();

    double cosTol = Math.Cos(angleToleranceDeg * Math.PI / 180.0);
    double touchTol = RevitUnitConverter.MmToFeet(50.0);

    XYZ chainStart = seedLoc.Curve.GetEndPoint(0);
    XYZ chainEnd = seedLoc.Curve.GetEndPoint(1);

    ExpandChainForward(chain, candidates, seedDir, ref chainEnd, cosTol, touchTol);
    ExpandChainBackward(chain, candidates, seedDir, ref chainStart, cosTol, touchTol);

    return chain;
}

private static void ExpandChainForward(
    List<FamilyInstance> chain, List<FamilyInstance> candidates,
    XYZ seedDir, ref XYZ chainEnd, double cosTol, double touchTol)
{
    bool added = true;
    while (added)
    {
        added = false;
        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            var cand = candidates[i];
            if (cand.Location is not LocationCurve cLoc) continue;

            XYZ p0 = cLoc.Curve.GetEndPoint(0);
            XYZ p1 = cLoc.Curve.GetEndPoint(1);
            XYZ dir = (p1 - p0).Normalize();
            if (Math.Abs(seedDir.DotProduct(dir)) < cosTol) continue;

            if (chainEnd.DistanceTo(p0) <= touchTol)
            {
                chain.Add(cand);
                chainEnd = p1;
                candidates.RemoveAt(i);
                added = true;
                break;
            }
            if (chainEnd.DistanceTo(p1) <= touchTol)
            {
                chain.Add(cand);
                chainEnd = p0;
                candidates.RemoveAt(i);
                added = true;
                break;
            }
        }
    }
}

private static void ExpandChainBackward(
    List<FamilyInstance> chain, List<FamilyInstance> candidates,
    XYZ seedDir, ref XYZ chainStart, double cosTol, double touchTol)
{
    bool added = true;
    while (added)
    {
        added = false;
        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            var cand = candidates[i];
            if (cand.Location is not LocationCurve cLoc) continue;

            XYZ p0 = cLoc.Curve.GetEndPoint(0);
            XYZ p1 = cLoc.Curve.GetEndPoint(1);
            XYZ dir = (p1 - p0).Normalize();
            if (Math.Abs(seedDir.DotProduct(dir)) < cosTol) continue;

            if (chainStart.DistanceTo(p1) <= touchTol)
            {
                chain.Insert(0, cand);
                chainStart = p0;
                candidates.RemoveAt(i);
                added = true;
                break;
            }
            if (chainStart.DistanceTo(p0) <= touchTol)
            {
                chain.Insert(0, cand);
                chainStart = p1;
                candidates.RemoveAt(i);
                added = true;
                break;
            }
        }
    }
}
```

---

### 3.3 AutoCAD Controllers Remediation

#### 3.3.1 Shared In-Memory Registry (`BeamTakeoffRegistry.cs`)
Create `HPKata.AutoCad/Controllers/BeamTakeoffRegistry.cs` to hold active recognized beam layouts across commands:
```csharp
namespace HPKata.AutoCad.Controllers
{
    public static class BeamTakeoffRegistry
    {
        private static readonly Dictionary<string, BeamRebarLayout> Cache = new(StringComparer.OrdinalIgnoreCase);

        public static IReadOnlyCollection<BeamRebarLayout> GetAll() => Cache.Values;

        public static void Register(BeamRebarLayout layout)
        {
            if (layout != null && !string.IsNullOrWhiteSpace(layout.BeamName))
            {
                Cache[layout.BeamName] = layout;
            }
        }

        public static bool TryGet(string beamName, out BeamRebarLayout? layout) =>
            Cache.TryGetValue(beamName, out layout);

        public static void Clear() => Cache.Clear();
    }
}
```

#### 3.3.2 Real Entity Scanning & Table Generation (`BeamTakeoffController.cs`)
1. **Dynamic Table Population (`GenerateBeamScheduleTable`)**:
   - Query beam layouts from `BeamPersistenceController.LoadAllBeamsFromDwg(doc.Database)`. If empty, read from `BeamTakeoffRegistry.GetAll()`. If still empty, construct from active framing selection.
   - For each beam layout, generate rebar items via `BeamScheduleAggregator.AggregateSchedule(layout)`.
   - Calculate total rows dynamically ($N + 2$, where $N$ is count of items).
   - Populate table cells with real marks, diameters, cutting lengths, quantities, and weights.
   - Add companion method `ExportBeamScheduleToExcel(string filePath)` invoking `BeamPlanScheduleWorkbookGenerator.GenerateWorkbookFile`.
2. **Real Selection Scanning (`ScanFramingPlanRegion`)**:
   - Iterate over `psr.Value.GetObjectIds()`.
   - Filter `Line` and `Polyline` entities on framing layers (`DAM`, `BEAM`, or containing Kata XData).
   - Group collinear touching segments into continuous beam runs.
   - Register recognized runs in `BeamTakeoffRegistry`.
   - Output real counts:
     `$"\n[HPKata] quet_dam: Scanned {psr.Value.Count} entities. Recognized and cached {recognizedRuns.Count} continuous beam runs.\n"`
3. **ModelSpace Scanning (`ScanFramingPlanAll`)**:
   - Open `BlockTableRecord.ModelSpace` for read.
   - Iterate all entities in ModelSpace and collect framing lines/polylines.
   - Group into continuous beam axes and register in `BeamTakeoffRegistry`.
   - Output real counts:
     `$"\n[HPKata] quet_dam_all: Scanned {totalCount} model space entities. Cached {recognizedRuns.Count} continuous beam runs for batch detailing.\n"`

#### 3.3.3 True DWG Dictionary Persistence (`BeamPersistenceController.cs`)
1. **DWG Dictionary Loader (`LoadAllBeamsFromDwg`)**:
   ```csharp
   public static List<BeamRebarLayout> LoadAllBeamsFromDwg(Database db)
   {
       var layouts = new List<BeamRebarLayout>();
       using var tr = db.TransactionManager.StartTransaction();
       var nod = (DBDictionary)tr.GetObject(db.NamedObjectsDictionaryId, OpenMode.ForRead);
       if (!nod.Contains("KATA_BEAM_DATA")) return layouts;

       var beamDict = (DBDictionary)tr.GetObject(nod.GetAt("KATA_BEAM_DATA"), OpenMode.ForRead);
       foreach (DBDictionaryEntry entry in beamDict)
       {
           var xrec = (Xrecord)tr.GetObject(entry.Value, OpenMode.ForRead);
           if (xrec.Data == null) continue;
           var typedVals = xrec.Data.AsArray();
           if (typedVals.Length > 0 && typedVals[0].Value is string json)
           {
               var layout = BeamXRecordSerializer.DeserializeFromJson(json);
               if (layout != null) layouts.Add(layout);
           }
       }
       tr.Commit();
       return layouts;
   }
   ```
2. **Genuine Serialization (`SaveBeamRebarToDwg`)**:
   - Accepts an optional `BeamRebarLayout? layout`. If null, prompts user for beam name and fetches from `BeamTakeoffRegistry` or selected entity XData.
   - Saves to `KATA_BEAM_DATA` dictionary in DWG via `BeamXRecordSerializer.SerializeToJson`.
3. **Batch DWG Serialization (`SaveAllBeamsToDwg`)**:
   - Iterates all layouts in `BeamTakeoffRegistry.GetAll()`.
   - Saves all to `KATA_BEAM_DATA` extension dictionary in a single transaction.
   - Reports: `$"\n[HPKata] Save_thep_dam_all: Serialized {savedCount} beam models into DWG extension dictionary 'KATA_BEAM_DATA'.\n"`.
4. **Interactive Layer Addition (`AddRebarLayerInteractive`)**:
   - Prompts for beam name, layer position (Top/Bottom), bar count, and diameter.
   - Loads layout from DWG dictionary, appends `ExtraBarLayerModel`, reserializes to DWG dictionary.
   - Reports: `$"\n[HPKata] Add_thep_dam: Added {barCount}Ø{dia} {pos} extra layer to beam '{beamName}'.\n"`.

#### 3.3.4 Distributed Load Assignment (`BeamEtabsController.cs`)
Replace dummy stubs for `AssignDeadLoad` (`GanTai`) and `AssignLiveLoad` (`HoatTai`):
- Prompt user for distributed load intensity ($q$ in kN/m) and load case name.
- Prompt user to select target framing beam entities.
- For each selected entity, attach load attributes via `XDataService.WriteXData` under `KataXDataRecord` or write to an attached extension dictionary.
- Output real confirmation:
  `$"\n[HPKata] GanTai: Assigned dead load {loadIntensity:F1} kN/m to {psr.Value.Count} selected beam elements.\n"`

#### 3.3.5 Dynamic Inputs for Remaining Controllers
- `BeamShopController.cs`: Prompt user for total span length (default 15,000 mm) and main bar diameter (default 20 mm), or read from selected entity.
- `BeamSectionController.cs`: Prompt user for section dimensions $b \times h$ and cover, or read from active beam layout.
- `BeamIntersectionController.cs`: Prompt user for beam tag name/dimensions, or read from host layout.

---

## 4. Test Suite Implementation Plan

### 4.1 New Tests in `HPKata.Revit.Tests`

Create `HPKata.Revit.Tests/Services/FramingRebarGeometryTests.cs`:
1. `CalculateBottomBarEndpoints_CalculatesCorrectElevationBelowTopFace`:
   - Start point at $Z = 3000\text{ mm}$, beam height $H = 600\text{ mm}$, cover $c = 30\text{ mm}$.
   - Verify: Bottom bar $Z = 3000 - 600 + 30 = 2430\text{ mm}$ (Feet equivalent).
   - Assert $Z_{\text{bot}} < Z_{\text{top}}$ (never above top face!).
2. `CalculateTopBarEndpoints_CalculatesCorrectElevationBelowTopFace`:
   - Start point at $Z = 3000\text{ mm}$, cover $c = 30\text{ mm}$.
   - Verify: Top bar $Z = 3000 - 30 = 2970\text{ mm}$.
3. `CalculateTransverseOffsets_EvenlyDistributesBarsAcrossClearWidth`:
   - Test for $N = 1, 2, 3, 4$ bars with width $B = 300\text{ mm}$, cover $30\text{ mm}$ (clear width $240\text{ mm}$).
   - Verify symmetry around 0.0 and correct spacing.
4. `CalculateStirrupCorners_FormsClosedRectangularHoop`:
   - Verify 4 corners match $(x_L, z_T)$, $(x_R, z_T)$, $(x_R, z_B)$, $(x_L, z_B)$.
   - Verify hoop width is $b - 2c$ and height is $h - 2c$.
5. `CollinearChaining_BidirectionalSearch_HandlesInteriorSeedAndReversedDirections`:
   - Test chain assembly logic with 3 collinear segments where seed is segment 2 and segment 1 has reversed curve direction.

### 4.2 New Tests in `HPKata.AutoCad.Tests`

Create `HPKata.AutoCad.Tests/Controllers/BeamControllerTests.cs`:
1. `BeamTakeoffRegistry_RegistersAndRetrievesLayouts`:
   - Verify registration, query by name, and GetAll.
2. `BeamPersistence_DWGDictionary_RoundtripsLayoutViaSerializer`:
   - Create multi-span `BeamRebarLayout`.
   - Verify `BeamXRecordSerializer` serialization and deserialization retains all spans, stirrup zones, and continuous bars.
3. `BeamScheduleAggregator_PopulatesTableData_MatchesCoreModels`:
   - Feed `BeamRebarLayout` to `BeamScheduleAggregator.AggregateSchedule`.
   - Verify that generated schedule rows contain correct marks, diameters, and quantities.
4. `BeamPlanScheduleWorkbookGenerator_GeneratesValidExcelWorkbook`:
   - Generate workbook stream and verify sheets and 44 columns match Kata schema.
5. `AssignLoads_AttachesStructuredDataToEntities`:
   - Verify load values (dead/live) are correctly structured and serialized.

---

## 5. Clean Code Governance Compliance Matrix

| Rule | Requirement | Remediation Guarantee |
|---|---|---|
| **C2** | Max 300 lines per physical file | `FramingRebarGenerator.cs` trimmed by delegating geometry to `FramingRebarGeometryCalculator.cs`. All new files will be $< 150$ lines. |
| **M3** | Max 50 lines per method | Chaining logic in `RevitFramingAnalyzer.cs` split into `ExpandChainForward` and `ExpandChainBackward` ($< 35$ lines each). Stirrups and longitudinal bar creation isolated to helper methods ($< 35$ lines each). |
| **Q-B1** | 0 dead code, 0 commented-out code, 0 TODOs | Verified by ripgrep scan before completion. |
| **Q-B2** | 0 unannotated empty catch blocks | All catch blocks annotated with rationale. |
| **R5** | 304.8 constant isolated to `RevitUnitConverter.cs` | Strictly use `RevitUnitConverter.MmToFeet` and `FeetToMm`. |
| **Host Neutrality** | `HPKata.Core` has 0 CAD/Revit references | Zero changes to `HPKata.Core` project references. |
| **Concurrency / MTP** | Prevent process collision & hard errors | All test runs executed with `--disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-<guid>"`. |

---

## 6. Implementation Steps & Verification Plan

```
[Step 1] Create FramingRebarGeometryCalculator.cs in HPKata.Revit/Services/
[Step 2] Refactor FramingRebarGenerator.cs (fix Z offset, multi-bar distribution, stirrups)
[Step 3] Update RebarCreationService.cs to support RebarStyle.StirrupTie
[Step 4] Refactor RevitFramingAnalyzer.cs with bidirectional expansion helpers
[Step 5] Create BeamTakeoffRegistry.cs in HPKata.AutoCad/Controllers/
[Step 6] Refactor BeamTakeoffController.cs (real scanning, dynamic table, Excel export)
[Step 7] Refactor BeamPersistenceController.cs (DWG dict load/save, interactive add layer)
[Step 8] Refactor BeamEtabsController.cs (real dead/live load assignment via XData)
[Step 9] Refactor BeamShopController.cs, BeamSectionController.cs, BeamIntersectionController.cs (remove hardcoded values)
[Step 10] Implement FramingRebarGeometryTests.cs in HPKata.Revit.Tests/
[Step 11] Implement BeamControllerTests.cs in HPKata.AutoCad.Tests/
[Step 12] Run full solution build & tests with MTP isolation
[Step 13] Run clean code AST checks (C2, M3, Q-B1, Q-B2, R5)
```

**Verification Commands**:
```powershell
# 1. Build
dotnet build HPKata/HPKata.slnx --disable-build-servers -m:1

# 2. Test
$guid = [guid]::NewGuid().ToString()
dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$guid"

# 3. Clean Code Validation
# Verify line lengths <= 300, methods <= 50, 0 TODOs
```
