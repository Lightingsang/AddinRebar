# CAD Commands & Aliases Remediation Specification Plan (Milestone M4)

**Agent**: `explorer_m4_rem_cad`  
**Date**: 2026-10-17  
**Milestone**: M4 (Iteration 2 Remediation & Hardening)  
**Parent Agent**: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`  

---

## 1. Executive Summary

During Milestone M4 audit and functional review, two critical AutoCAD command defects were identified:
1. **Facade Violation in `dnoi` / `dim_noi_tiep` (`DimensionUtilityCommands.cs:26-31`)**:
   Continuous Dimensioning command delegated directly to `DimensionUtilityController.TrimDimensionWitnessLinesInteractive()`, executing witness line trimming (identical to `catdim`) instead of continuous chained dimensioning.
2. **Missing Command Registration for `ve_grid` (`LayoutCommands.cs:12-17`)**:
   `GoiLenh_HPKata.lsp:43` defines `(defun c:vg()(command "ve_grid"))`. `LayoutCommands.cs` only registered `vg` and `ve_luoi_truc`, causing AutoCAD users typing `vg` to experience runtime failure `Unknown command "VE_GRID"`.

This specification provides a zero-facade, genuine implementation plan for both issues while strictly adhering to Clean Code Governance rules (Rule C2 $\le 300$ lines/file, Rule M3 $\le 50$ lines/method, Rule Q-B1 zero dead code, Rule Q-B2 annotated catches).

---

## 2. Features Discovered

| # | Category | Feature | Description | Inputs | Outputs | Error Behavior | Discovered Via |
|---|----------|---------|-------------|--------|---------|----------------|----------------|
| 1 | CAD Dimensioning | `ContinueDimensionInteractive` (`dnoi` / `dim_noi_tiep`) | Interactive continuous dimension chaining. Prompts user to select an existing base dimension (`AlignedDimension` or `RotatedDimension`) or pick two starting points, then loops allowing consecutive points to chain dimensions along the same elevation and rotation angle. | Base dimension selection or 2 seed points + dim line point; consecutive target points $P_3, P_4, \dots$ | Chained `AlignedDimension` or `RotatedDimension` entities appended to current `BlockTableRecord` (ModelSpace) with inherited style and layer | Cancel/ESC or Enter exits loop gracefully, committing placed dimensions; degenerate (<0.1mm) points skipped | `auditor_m4/audit_report.md:26`, `reviewer_m4_2/review.md:16`, `DimensionUtilityCommands.cs:26-31` |
| 2 | CAD Command Aliases | `[CommandMethod("ve_grid")]` (`LayoutCommands.cs`) | Command registration mapping `ve_grid` directly to `GridDrawController.DrawGridInteractive()`. Satisfies AutoCAD Lisp alias `c:vg` in `GoiLenh_HPKata.lsp:43`. | Command line invocation `ve_grid` (or `vg` via Lisp) | Opens Grid Generator interactive workflow / draws structural grid axes | Throws no exception; exits safely if canceled or out-of-process | `auditor_m4/audit_report.md:28`, `reviewer_m4_2/review.md:17`, `GoiLenh_HPKata.lsp:43` |
| 3 | CAD Dimensioning | Rotated Dimension Chaining Support | Detects if base dimension is `RotatedDimension` (e.g. from `DIMLINEAR`) and preserves `Rotation` angle across continuous segments. | `RotatedDimension` object | Chained `RotatedDimension` entities sharing rotation angle and dim line point | Falls back to `AlignedDimension` if base is not `RotatedDimension` | Codebase inspection (`ColumnDimensionController.cs:40`) |
| 4 | CAD Dimensioning | Visual Rubber-Band Guidance | Utilizes `PromptPointOptions.UseBasePoint` and `BasePoint` during continuation loop to provide real-time visual rubber-band line in AutoCAD. | Prior extension point $P_{curr}$ | Real-time dynamic preview line to cursor | Ignored gracefully in headless/test environments | AutoCAD .NET Editor API |

---

## 3. Edge Cases

| # | Feature | Input | Observed / Specified Behavior |
|---|---------|-------|-------------------------------|
| 1 | `ContinueDimensionInteractive` | `doc == null` (Out-of-process test execution) | Early return without throwing `NullReferenceException`. Safe for unit tests. |
| 2 | `ContinueDimensionInteractive` | User presses ESC / Cancel on base dimension prompt | `PromptStatus.Cancel` terminates command cleanly. 0 entities created, no dirty transaction. |
| 3 | `ContinueDimensionInteractive` | User presses Enter on base prompt (`PromptStatus.None`) to create new chain | Prompts for 2 origin points and dim line position. Creates first `AlignedDimension`, then seamlessly continues chaining. |
| 4 | `ContinueDimensionInteractive` | User cancels during seed point prompt (Point 1, 2, or DimLine) | Returns false, commits 0 entities, prints no error. |
| 5 | `ContinueDimensionInteractive` | User clicks multiple identical/coincident points (<0.1mm apart) | Filter checks `nextPt.DistanceTo(currentStart) < 1e-4` and ignores degenerate point, awaiting next valid click. |
| 6 | `ContinueDimensionInteractive` | User clicks 3 valid points then presses ESC | `PromptStatus.Cancel` breaks loop; `tr.Commit()` commits the 3 dimensions already placed, reporting `Đã tạo nối tiếp 3 đường kích thước.` |
| 7 | `ContinueDimensionInteractive` | Selected base dimension is on custom layer (e.g. `kata_dim`) and has custom DimStyle | New chained dimensions inherit `Layer` and `DimensionStyle` from the base dimension. |
| 8 | `DrawGrid` (`ve_grid`) | Invoked via Lisp `c:vg` -> `(command "ve_grid")` | AutoCAD successfully dispatches `ve_grid` without `Unknown command` error. |

---

## 4. Remediation Plan & Exact Implementation Code

### 4.1 Dimension Utility Commands (`DimensionUtilityCommands.cs`)
- **File**: `HPKata/HPKata.AutoCad/Commands/DimensionUtilityCommands.cs`
- **Current Lines**: 41 lines
- **Change**: Replace line 30 `DimensionUtilityController.TrimDimensionWitnessLinesInteractive();` with `DimensionUtilityController.ContinueDimensionInteractive();`.
- **Target Line Count**: 41 lines (Budget: $\le 300$ lines, Margin: 259 lines).

```csharp
        [CommandMethod("dnoi")]
        [CommandMethod("dim_noi_tiep")]
        public static void ContinueDimension()
        {
            DimensionUtilityController.ContinueDimensionInteractive();
        }
```

### 4.2 Layout Commands (`LayoutCommands.cs`)
- **File**: `HPKata/HPKata.AutoCad/Commands/LayoutCommands.cs`
- **Current Lines**: 66 lines
- **Change**: Add `[CommandMethod("ve_grid")]` attribute to `DrawGrid()`.
- **Target Line Count**: 67 lines (Budget: $\le 300$ lines, Margin: 233 lines).

```csharp
        [CommandMethod("vg")]
        [CommandMethod("ve_grid")]
        [CommandMethod("ve_luoi_truc")]
        public static void DrawGrid()
        {
            GridDrawController.DrawGridInteractive();
        }
```

### 4.3 Dimension Utility Controller (`DimensionUtilityController.cs`)
- **File**: `HPKata/HPKata.AutoCad/Controllers/DimensionUtilityController.cs`
- **Current Lines**: 131 lines
- **Proposed Additions**: 138 lines (New total: 269 lines $\le 300$ lines).
- **Methods to Add**:
  1. `public static void ContinueDimensionInteractive()` (26 lines $\le 50$)
  2. `private static bool TryGetBaseDimensionInfo(...)` (35 lines $\le 50$)
  3. `private static bool TryPromptNewBaseDimension(...)` (25 lines $\le 50$)
  4. `private static int LoopContinuousDimensions(...)` (35 lines $\le 50$)
  5. `private readonly struct BaseDimInfo` (17 lines)

#### Complete C# Implementation for `DimensionUtilityController.cs`:

```csharp
        public static void ContinueDimensionInteractive()
        {
            var doc = AcadApp.DocumentManager.MdiActiveDocument;
            if (doc == null) return;
            var ed = doc.Editor;

            using (doc.LockDocument())
            using (var tr = doc.Database.TransactionManager.StartTransaction())
            {
                if (!TryGetBaseDimensionInfo(doc, ed, tr, out var baseInfo, out bool initialCreated))
                {
                    return;
                }

                var curBtr = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite);
                int createdCount = LoopContinuousDimensions(ed, tr, curBtr, baseInfo);

                if (createdCount > 0 || initialCreated)
                {
                    tr.Commit();
                    ed.WriteMessage($"\n[HPKata Dim] Đã tạo nối tiếp {createdCount} đường kích thước.\n");
                }
            }
        }

        private static bool TryGetBaseDimensionInfo(Document doc, Editor ed, Transaction tr, out BaseDimInfo baseInfo, out bool initialCreated)
        {
            baseInfo = default;
            initialCreated = false;

            var peo = new PromptEntityOptions("\n[HPKata Dim] Chọn đường kích thước cơ sở (hoặc Enter để chọn 2 điểm mới): ")
            {
                AllowNone = true
            };
            peo.SetRejectMessage("\nChỉ chọn đối tượng Dimension.");
            peo.AddAllowedClass(typeof(Dimension), true);
            var per = ed.GetEntity(peo);

            if (per.Status == PromptStatus.OK)
            {
                var ent = tr.GetObject(per.ObjectId, OpenMode.ForRead);
                if (ent is RotatedDimension rotDim)
                {
                    baseInfo = new BaseDimInfo(rotDim.XLine2Point, rotDim.DimLinePoint, rotDim.DimensionStyle, rotDim.Layer, rotDim.Rotation);
                    return true;
                }
                if (ent is AlignedDimension alDim)
                {
                    baseInfo = new BaseDimInfo(alDim.XLine2Point, alDim.DimLinePoint, alDim.DimensionStyle, alDim.Layer);
                    return true;
                }
            }

            if (per.Status == PromptStatus.None)
            {
                return TryPromptNewBaseDimension(doc, ed, tr, out baseInfo, out initialCreated);
            }

            return false;
        }

        private static bool TryPromptNewBaseDimension(Document doc, Editor ed, Transaction tr, out BaseDimInfo baseInfo, out bool initialCreated)
        {
            baseInfo = default;
            initialCreated = false;

            var p1 = ed.GetPoint("\n[HPKata Dim] Chọn điểm gốc thứ nhất: ");
            if (p1.Status != PromptStatus.OK) return false;

            var p2 = ed.GetPoint("\n[HPKata Dim] Chọn điểm gốc thứ hai: ");
            if (p2.Status != PromptStatus.OK) return false;

            var pDim = ed.GetPoint("\n[HPKata Dim] Chọn vị trí đường kích thước: ");
            if (pDim.Status != PromptStatus.OK) return false;

            var curBtr = (BlockTableRecord)tr.GetObject(doc.Database.CurrentSpaceId, OpenMode.ForWrite);
            var firstDim = new AlignedDimension(p1.Value, p2.Value, pDim.Value, "", doc.Database.Dimstyle);
            curBtr.AppendEntity(firstDim);
            tr.AddNewlyCreatedDBObject(firstDim, true);

            baseInfo = new BaseDimInfo(p2.Value, pDim.Value, doc.Database.Dimstyle, firstDim.Layer);
            initialCreated = true;
            return true;
        }

        private static int LoopContinuousDimensions(Editor ed, Transaction tr, BlockTableRecord curBtr, BaseDimInfo baseInfo)
        {
            int count = 0;
            var currentStart = baseInfo.StartPt;

            while (true)
            {
                var ppo = new PromptPointOptions("\n[HPKata Dim] Chọn điểm tiếp theo (hoặc Enter/ESC để kết thúc): ")
                {
                    UseBasePoint = true,
                    BasePoint = currentStart
                };
                var ppr = ed.GetPoint(ppo);
                if (ppr.Status != PromptStatus.OK) break;

                var nextPt = ppr.Value;
                if (nextPt.DistanceTo(currentStart) < 1e-4) continue;

                Dimension newDim = baseInfo.Rotation.HasValue
                    ? new RotatedDimension(baseInfo.Rotation.Value, currentStart, nextPt, baseInfo.DimLinePt, "", baseInfo.DimStyleId)
                    : new AlignedDimension(currentStart, nextPt, baseInfo.DimLinePt, "", baseInfo.DimStyleId);

                if (!string.IsNullOrEmpty(baseInfo.Layer))
                {
                    newDim.Layer = baseInfo.Layer;
                }

                curBtr.AppendEntity(newDim);
                tr.AddNewlyCreatedDBObject(newDim, true);
                count++;
                currentStart = nextPt;
            }

            return count;
        }

        private readonly struct BaseDimInfo
        {
            public Point3d StartPt { get; }
            public Point3d DimLinePt { get; }
            public ObjectId DimStyleId { get; }
            public string Layer { get; }
            public double? Rotation { get; }

            public BaseDimInfo(Point3d startPt, Point3d dimLinePt, ObjectId dimStyleId, string layer, double? rotation = null)
            {
                StartPt = startPt;
                DimLinePt = dimLinePt;
                DimStyleId = dimStyleId;
                Layer = layer;
                Rotation = rotation;
            }
        }
```

---

## 5. Automated Test Specifications

Update `HPKata/HPKata.AutoCad.Tests/Controllers/Phase5AutoCadTests.cs`:

1. **Verify `ve_grid` in `LayoutCommands_AllCommandMethodsAreRegistered`**:
   ```csharp
   Assert.Contains("vg", commandNames);
   Assert.Contains("ve_grid", commandNames);
   Assert.Contains("ve_luoi_truc", commandNames);
   ```

2. **Verify `ContinueDimensionInteractive` out-of-process safety in `CadUtilitiesControllers_SafeExecutionWithoutActiveDoc`**:
   ```csharp
   DimensionUtilityController.TrimDimensionWitnessLinesInteractive();
   DimensionUtilityController.SplitDimensionInteractive();
   DimensionUtilityController.ContinueDimensionInteractive();
   DimensionUtilityController.DrawSlopeAnnotationInteractive();
   ```

3. **Dedicated Attribute Registration Tests**:
   ```csharp
   [Fact]
   public void LayoutCommands_DrawGrid_RegistersAllExpectedAliases()
   {
       var method = typeof(LayoutCommands).GetMethod("DrawGrid", BindingFlags.Public | BindingFlags.Static);
       Assert.NotNull(method);
       var names = method.GetCustomAttributes<CommandMethodAttribute>().Select(a => a.GlobalName).ToList();
       Assert.Contains("vg", names);
       Assert.Contains("ve_grid", names);
       Assert.Contains("ve_luoi_truc", names);
   }

   [Fact]
   public void DimensionUtilityCommands_ContinueDimension_RegistersExpectedAliases()
   {
       var method = typeof(DimensionUtilityCommands).GetMethod("ContinueDimension", BindingFlags.Public | BindingFlags.Static);
       Assert.NotNull(method);
       var names = method.GetCustomAttributes<CommandMethodAttribute>().Select(a => a.GlobalName).ToList();
       Assert.Contains("dnoi", names);
       Assert.Contains("dim_noi_tiep", names);
   }
   ```

---

## 6. Clean Code Compliance Verification

| Clean Code Rule | Requirement | Target Status | Verification |
|-----------------|-------------|---------------|--------------|
| **Rule C2** | File length $\le 300$ physical lines | `DimensionUtilityCommands.cs`: 41 lines<br>`LayoutCommands.cs`: 67 lines<br>`DimensionUtilityController.cs`: 269 lines | PASS |
| **Rule M3** | Method length $\le 50$ physical lines | `ContinueDimensionInteractive`: 26 lines<br>`TryGetBaseDimensionInfo`: 35 lines<br>`TryPromptNewBaseDimension`: 25 lines<br>`LoopContinuousDimensions`: 35 lines | PASS |
| **Rule Q-B1** | 0 dead / commented-out code | Zero commented lines, 0 TODOs | PASS |
| **Rule Q-B2** | 0 unannotated catch blocks | No catch blocks in these methods (uses safe early returns & disposables) | PASS |
| **Rule R5** | Isolation of `304.8` | No numeric constants or Revit conversion factors in CAD code | PASS |
| **Architecture** | Host dependency isolation | CAD host commands only reference AutoCAD runtime/DB services | PASS |

---

## 7. Independent Verification Commands

To compile and verify without process locks or `Unknown Hard Error` popups:

```powershell
# 1. Build HPKata solution
dotnet build HPKata/HPKata.slnx --disable-build-servers -m:1

# 2. Run AutoCad tests with unique MTP results directory
dotnet test HPKata/HPKata.AutoCad.Tests/HPKata.AutoCad.Tests.csproj --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"

# 3. Full solution test run
dotnet test HPKata/HPKata.slnx --disable-build-servers -m:1 --results-directory "$env:TEMP\mtp-$([guid]::NewGuid())"
```
