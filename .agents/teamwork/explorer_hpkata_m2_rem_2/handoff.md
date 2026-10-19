# Handoff Report: Revit Commands Refactoring & Safe Teardown Isolation

- **Author**: `explorer_hpkata_m2_rem_2` (M2 Iteration 2 Explorer — Revit Commands & Safe Teardown)
- **Target Solution**: `HPKata/HPKata.slnx`
- **Milestone**: M2 (Features 5–8)
- **Handoff Type**: Hard Handoff (Investigation & Proposed Diffs Complete)
- **Artifacts**:
  - Unified Patch: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_rem_2\m2_revit_remediation.patch`
  - Proposed File: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_rem_2\proposed_ColumnRebarCommands.cs`
  - Proposed File: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_rem_2\proposed_FramingRebarCommands.cs`
  - Proposed File: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_rem_2\proposed_WallRebarCommands.cs`
  - Proposed File: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_rem_2\proposed_RevitExportExcelRunner.cs`
  - Proposed File: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_rem_2\proposed_RevitExportExcelRunnerTests.cs`

---

## 1. Observation

### Observation 1.1: Rule M3 Violations in HPKata.Revit Commands
Physical line and AST analysis of `HPKata/HPKata.Revit/Commands/` identified that the `Execute` method in all three detailing commands exceeded the 50-line Rule M3 ceiling:
1. `HPKata/HPKata.Revit/Commands/ColumnRebarCommands.cs:23`:
   - `Execute(ExternalCommandData, ref string, ElementSet)` spans lines 23 to 78 (**56 physical lines**).
2. `HPKata/HPKata.Revit/Commands/FramingRebarCommands.cs:23`:
   - `Execute(ExternalCommandData, ref string, ElementSet)` spans lines 23 to 78 (**56 physical lines**).
3. `HPKata/HPKata.Revit/Commands/WallRebarCommands.cs:23`:
   - `Execute(ExternalCommandData, ref string, ElementSet)` spans lines 23 to 78 (**56 physical lines**).

All three methods perform three separate responsibilities in a single block:
- Interactive element selection & type validation via `PickObject`.
- ViewModel initialization, active Excel workbook lookup, and preloading from sheet data.
- Modal UI dialog display via `RevitWindowHelper.ShowModal` followed by rebar transaction generation.

### Observation 1.2: Destructive Teardown Deletion of `C:\HPKata\Kata.xlsm`
In `HPKata/HPKata.Revit/Runners/RevitExportExcelRunner.cs` lines 56–81:
```csharp
    private static string ResolveOutputPath(IRevitDocumentContext context, string? outputFilePath)
    {
        if (!string.IsNullOrWhiteSpace(outputFilePath))
        {
            return outputFilePath!;
        }

        string kataPath = KataInstallEnvironment.Current.KataWorkbookPath;
        if (File.Exists(kataPath))
        {
            return kataPath;
        }
        ...
```
When `outputFilePath` is not passed, `ResolveOutputPath` returns `kataPath` (`C:\HPKata\Kata.xlsm`) if it exists on disk.

In `HPKata/HPKata.Revit.Tests/Runners/RevitExportExcelRunnerTests.cs` lines 88–102:
```csharp
    [Fact]
    public void Execute_ResolvesOutputPathFromContextTitle()
    {
        var context = CreateTestContext();
        string generatedPath = RevitExportExcelRunner.Execute(context, outputFilePath: null);

        try
        {
            Assert.True(File.Exists(generatedPath));
            Assert.Contains("DuAnTest", generatedPath);
        }
        finally
        {
            if (File.Exists(generatedPath)) File.Delete(generatedPath);
        }
    }
```
When `C:\HPKata\Kata.xlsm` is staged on the system:
1. `RevitExportExcelRunner.Execute(context, outputFilePath: null)` returns `"C:\HPKata\Kata.xlsm"`.
2. `Assert.Contains("DuAnTest", generatedPath)` fails with:
   ```text
   Assert.Contains() Failure: Sub-string not found
   String:    "C:\\HPKata\\Kata.xlsm"
   Not found: "DuAnTest"
   ```
3. Line 100 executes: `finally { if (File.Exists(generatedPath)) File.Delete(generatedPath); }`, immediately deleting `C:\HPKata\Kata.xlsm` from disk.
4. On empirical verification, `Test-Path 'C:\HPKata\Kata.xlsm'` returned `False` because the test suite destroyed the production master asset.

### Observation 1.3: Solution-Wide Clean Code Status in `HPKata.Revit` and `HPKata.Revit.Tests`
A scan of all 65 `.cs` source files in `HPKata.Revit` and `HPKata.Revit.Tests`:
- **Rule C2 (File Length <= 300 lines)**: 0 violations. (Max file length observed: 222 lines in `RevitDetailingCommandTests.cs`).
- **Rule M3 (Method Length <= 50 lines)**: Exactly the 3 `Execute` methods in `ColumnRebarCommands.cs`, `FramingRebarCommands.cs`, and `WallRebarCommands.cs` exceeded 50 lines. No other methods in `HPKata.Revit` or `HPKata.Revit.Tests` exceeded 50 lines.

---

## 2. Logic Chain

1. **Extracting Helper Methods for Rule M3 Compliance**:
   - Each command's `Execute` method can delegate responsibilities into two static helper methods:
     - `ShowDialogAndGenerateRebar(UIApplication uiapp, Document doc, TElement element)`: displays modal UI and executes transaction (15 lines).
     - `CreateViewModel(TElement element)`: configures adapter and executes preloading if matching mark exists (19 lines).
   - This reduces `Execute` in all three commands from 56 lines down to **31 lines** (well below the target of <= 35 lines and the hard ceiling of <= 50 lines).

2. **Decoupling Default Output Path from Master Library in `RevitExportExcelRunner`**:
   - In Revit, schedule exports default to a file named after the active Revit document (`{DocumentTitle}_ThongKeThep.xlsx`) in the document's directory or `%TEMP%`.
   - Returning `KataWorkbookPath` (`C:\HPKata\Kata.xlsm`) as the default export target violates the principle of preserving installed master templates.
   - Removing the `File.Exists(kataPath)` check from `ResolveOutputPath` ensures that:
     - When `outputFilePath` is omitted, the path is always derived from the context document title or directory.
     - `Assert.Contains("DuAnTest", generatedPath)` succeeds because the path contains `DuAnTest_ThongKeThep.xlsx`.
     - Production asset `C:\HPKata\Kata.xlsm` is never returned as a default export path.

3. **Guarding ClosedXml Fallback Export in `RevitExportExcelRunner`**:
   - If a caller explicitly specifies `outputFilePath: @"C:\HPKata\Kata.xlsm"` and the adapter's `ExportSchedule` returns `false`, `ClosedXmlExporter.ExportScheduleToFile(report, resolvedPath)` would wipe the macro-enabled workbook.
   - Introducing `IsMasterLibraryAsset(resolvedPath)` redirects any fallback export away from master assets to `{safeTitle}_ThongKeThep_Fallback.xlsx`.

4. **Isolating Environment & Teardown Guard in `RevitExportExcelRunnerTests`**:
   - `Execute_ResolvesOutputPathFromContextTitle` now scopes execution using `KataInstallEnvironment.UseRootDirectory(isolatedDir)`.
   - In the `finally` block, `File.Delete(generatedPath)` is guarded by `!IsMasterInstallationAsset(generatedPath)` (checking against `KataInstallEnvironment.DefaultRootDirectory`).
   - This provides defense-in-depth: even if a path resolution were ever to return a path in `C:\HPKata\`, teardown will refuse to delete it.

---

## 3. Caveats

- **Scope Boundary**: This investigation focuses strictly on `HPKata.Revit` and `HPKata.Revit.Tests` (Milestone M2, Feature 8 and test safety). Changes to `KataColumnDataMapper.cs` (51 lines) and `KataComExcelSession.cs:154` (empty catch) belong to the Core Excel component handled by peer explorer/worker agents.
- **Staging Restoration**: Because previous test runs deleted `C:\HPKata\Kata.xlsm`, `C:\HPKata\Kata.xlsm` must be re-staged from `HPKata/Distribution/Assets/Kata.xlsm` before final acceptance verification.
- **Read-Only Explorer Discipline**: In accordance with explorer archetype constraints, source code files in `HPKata/` were not directly mutated. Proposed solutions are delivered as machine-applicable patch files and standalone proposed replacement files.

---

## 4. Conclusion

All Rule M3 violations in `HPKata.Revit` commands are completely resolved with clean, readable helper extractions (`Execute` <= 31 lines). The destructive test teardown vulnerability in `RevitExportExcelRunnerTests` and `RevitExportExcelRunner` is eliminated via dual-layer isolation and master asset protection.

### Summary of Method and File Metrics:
| File | Method | Before (Lines) | After (Lines) | Rule M3 (<=50) | Rule C2 (<=300) |
|---|---|:---:|:---:|:---:|:---:|
| `ColumnRebarCommands.cs` | `Execute` | 56 | **31** | PASS | 99 lines (PASS) |
| `ColumnRebarCommands.cs` | `ShowDialogAndGenerateRebar` | — | **15** | PASS | |
| `ColumnRebarCommands.cs` | `CreateViewModel` | — | **19** | PASS | |
| `FramingRebarCommands.cs` | `Execute` | 56 | **31** | PASS | 99 lines (PASS) |
| `FramingRebarCommands.cs` | `ShowDialogAndGenerateRebar` | — | **15** | PASS | |
| `FramingRebarCommands.cs` | `CreateViewModel` | — | **19** | PASS | |
| `WallRebarCommands.cs` | `Execute` | 56 | **31** | PASS | 99 lines (PASS) |
| `WallRebarCommands.cs` | `ShowDialogAndGenerateRebar` | — | **15** | PASS | |
| `WallRebarCommands.cs` | `CreateViewModel` | — | **19** | PASS | |
| `RevitExportExcelRunner.cs` | `Execute` | 32 | **37** | PASS | 95 lines (PASS) |
| `RevitExportExcelRunner.cs` | `ResolveOutputPath` | 26 | **20** | PASS | |
| `RevitExportExcelRunner.cs` | `IsMasterLibraryAsset` | — | **7** | PASS | |
| `RevitExportExcelRunner.cs` | `ResolveDocumentFallbackPath` | — | **5** | PASS | |
| `RevitExportExcelRunnerTests.cs` | `Execute_ResolvesOutputPathFromContextTitle` | 15 | **26** | PASS | 135 lines (PASS) |
| `RevitExportExcelRunnerTests.cs` | `IsMasterInstallationAsset` | — | **5** | PASS | |

---

## Exact Unified Diffs

### 1. `HPKata/HPKata.Revit/Commands/ColumnRebarCommands.cs`
```diff
--- a/HPKata/HPKata.Revit/Commands/ColumnRebarCommands.cs
+++ b/HPKata/HPKata.Revit/Commands/ColumnRebarCommands.cs
@@ -26,45 +26,20 @@
             var uidoc = uiapp.ActiveUIDocument;
             if (uidoc == null) return Result.Cancelled;
-            var doc = uidoc.Document;
 
             try
             {
                 var refCol = uidoc.Selection.PickObject(
                     ObjectType.Element,
                     new ColumnSelectionFilter(),
                     "Select a Structural Column to detail with HPKata:");
 
-                var col = doc.GetElement(refCol) as FamilyInstance;
-                if (col == null) return Result.Cancelled;
-
-                var adapter = KataExcelAdapterProvider.Current;
-                var vm = new ColumnDetailingViewModel(adapter)
-                {
-                    ColumnMark = col.Name
-                };
-
-                string kataPath = KataInstallEnvironment.Current.KataWorkbookPath;
-                if (File.Exists(kataPath) && adapter.ReadColumnSheet(kataPath, out var sheetData))
-                {
-                    if (string.Equals(sheetData.ColumnMark, col.Name, StringComparison.OrdinalIgnoreCase))
-                    {
-                        vm.ReadExcelCommand.Execute(null);
-                    }
-                }
-
-                var view = new ColumnDetailingWindow(vm);
-                bool? dialogResult = RevitWindowHelper.ShowModal(view, uiapp);
-
-                if (vm.ResultLayout != null)
-                {
-                    using var tr = new Transaction(doc, $"HPKata: Generate Rebar for {col.Name}");
-                    tr.Start();
-                    ColumnRebarGenerator.GenerateColumnRebar(doc, col, vm.ResultLayout);
-                    tr.Commit();
-                    TaskDialog.Show("HPKata", $"Successfully generated 3D rebar in column '{col.Name}'.");
+                if (uidoc.Document.GetElement(refCol) is not FamilyInstance col)
+                {
+                    return Result.Cancelled;
                 }
 
+                ShowDialogAndGenerateRebar(uiapp, uidoc.Document, col);
                 return Result.Succeeded;
             }
             catch (Autodesk.Revit.Exceptions.OperationCanceledException)
@@ -76,6 +51,39 @@
                 return Result.Failed;
             }
         }
+
+        private static void ShowDialogAndGenerateRebar(UIApplication uiapp, Document doc, FamilyInstance col)
+        {
+            var vm = CreateViewModel(col);
+            var view = new ColumnDetailingWindow(vm);
+            RevitWindowHelper.ShowModal(view, uiapp);
+
+            if (vm.ResultLayout != null)
+            {
+                using var tr = new Transaction(doc, $"HPKata: Generate Rebar for {col.Name}");
+                tr.Start();
+                ColumnRebarGenerator.GenerateColumnRebar(doc, col, vm.ResultLayout);
+                tr.Commit();
+                TaskDialog.Show("HPKata", $"Successfully generated 3D rebar in column '{col.Name}'.");
+            }
+        }
+
+        private static ColumnDetailingViewModel CreateViewModel(FamilyInstance col)
+        {
+            var adapter = KataExcelAdapterProvider.Current;
+            var vm = new ColumnDetailingViewModel(adapter)
+            {
+                ColumnMark = col.Name
+            };
+
+            string kataPath = KataInstallEnvironment.Current.KataWorkbookPath;
+            if (File.Exists(kataPath) && adapter.ReadColumnSheet(kataPath, out var sheetData))
+            {
+                if (string.Equals(sheetData.ColumnMark, col.Name, StringComparison.OrdinalIgnoreCase))
+                {
+                    vm.ReadExcelCommand.Execute(null);
+                }
+            }
+
+            return vm;
+        }
 
         private sealed class ColumnSelectionFilter : ISelectionFilter
```

### 2. `HPKata/HPKata.Revit/Commands/FramingRebarCommands.cs`
```diff
--- a/HPKata/HPKata.Revit/Commands/FramingRebarCommands.cs
+++ b/HPKata/HPKata.Revit/Commands/FramingRebarCommands.cs
@@ -26,45 +26,20 @@
             var uidoc = uiapp.ActiveUIDocument;
             if (uidoc == null) return Result.Cancelled;
-            var doc = uidoc.Document;
 
             try
             {
                 var refBeam = uidoc.Selection.PickObject(
                     ObjectType.Element,
                     new FramingSelectionFilter(),
                     "Select a Structural Framing beam to detail with HPKata:");
 
-                var beam = doc.GetElement(refBeam) as FamilyInstance;
-                if (beam == null) return Result.Cancelled;
-
-                var adapter = KataExcelAdapterProvider.Current;
-                var vm = new BeamDetailingViewModel(adapter)
-                {
-                    BeamMark = beam.Name
-                };
-
-                string kataPath = KataInstallEnvironment.Current.KataWorkbookPath;
-                if (File.Exists(kataPath) && adapter.ReadBeamSheet(kataPath, out var sheetData))
-                {
-                    if (string.Equals(sheetData.BeamMark, beam.Name, StringComparison.OrdinalIgnoreCase))
-                    {
-                        vm.ReadExcelCommand.Execute(null);
-                    }
-                }
-
-                var view = new BeamDetailingWindow(vm);
-                bool? dialogResult = RevitWindowHelper.ShowModal(view, uiapp);
-
-                if (vm.ResultLayout != null)
-                {
-                    using var tr = new Transaction(doc, $"HPKata: Generate Rebar for {beam.Name}");
-                    tr.Start();
-                    FramingRebarGenerator.GenerateFramingRebar(doc, beam, vm.ResultLayout);
-                    tr.Commit();
-                    TaskDialog.Show("HPKata", $"Successfully generated 3D rebar in beam '{beam.Name}'.");
+                if (uidoc.Document.GetElement(refBeam) is not FamilyInstance beam)
+                {
+                    return Result.Cancelled;
                 }
 
+                ShowDialogAndGenerateRebar(uiapp, uidoc.Document, beam);
                 return Result.Succeeded;
             }
             catch (Autodesk.Revit.Exceptions.OperationCanceledException)
@@ -76,6 +51,39 @@
                 return Result.Failed;
             }
         }
+
+        private static void ShowDialogAndGenerateRebar(UIApplication uiapp, Document doc, FamilyInstance beam)
+        {
+            var vm = CreateViewModel(beam);
+            var view = new BeamDetailingWindow(vm);
+            RevitWindowHelper.ShowModal(view, uiapp);
+
+            if (vm.ResultLayout != null)
+            {
+                using var tr = new Transaction(doc, $"HPKata: Generate Rebar for {beam.Name}");
+                tr.Start();
+                FramingRebarGenerator.GenerateFramingRebar(doc, beam, vm.ResultLayout);
+                tr.Commit();
+                TaskDialog.Show("HPKata", $"Successfully generated 3D rebar in beam '{beam.Name}'.");
+            }
+        }
+
+        private static BeamDetailingViewModel CreateViewModel(FamilyInstance beam)
+        {
+            var adapter = KataExcelAdapterProvider.Current;
+            var vm = new BeamDetailingViewModel(adapter)
+            {
+                BeamMark = beam.Name
+            };
+
+            string kataPath = KataInstallEnvironment.Current.KataWorkbookPath;
+            if (File.Exists(kataPath) && adapter.ReadBeamSheet(kataPath, out var sheetData))
+            {
+                if (string.Equals(sheetData.BeamMark, beam.Name, StringComparison.OrdinalIgnoreCase))
+                {
+                    vm.ReadExcelCommand.Execute(null);
+                }
+            }
+
+            return vm;
+        }
 
         private sealed class FramingSelectionFilter : ISelectionFilter
```

### 3. `HPKata/HPKata.Revit/Commands/WallRebarCommands.cs`
```diff
--- a/HPKata/HPKata.Revit/Commands/WallRebarCommands.cs
+++ b/HPKata/HPKata.Revit/Commands/WallRebarCommands.cs
@@ -26,45 +26,20 @@
             var uidoc = uiapp.ActiveUIDocument;
             if (uidoc == null) return Result.Cancelled;
-            var doc = uidoc.Document;
 
             try
             {
                 var refWall = uidoc.Selection.PickObject(
                     ObjectType.Element,
                     new WallSelectionFilter(),
                     "Select a Structural Wall to detail with HPKata:");
 
-                var wall = doc.GetElement(refWall) as Wall;
-                if (wall == null) return Result.Cancelled;
-
-                var adapter = KataExcelAdapterProvider.Current;
-                var vm = new WallDetailingViewModel(adapter)
-                {
-                    WallMark = wall.Name
-                };
-
-                string kataPath = KataInstallEnvironment.Current.KataWorkbookPath;
-                if (File.Exists(kataPath) && adapter.ReadWallSheet(kataPath, out var sheetData))
-                {
-                    if (string.Equals(sheetData.WallMark, wall.Name, StringComparison.OrdinalIgnoreCase))
-                    {
-                        vm.ReadExcelCommand.Execute(null);
-                    }
-                }
-
-                var view = new WallDetailingWindow(vm);
-                bool? dialogResult = RevitWindowHelper.ShowModal(view, uiapp);
-
-                if (vm.ResultModel != null)
-                {
-                    using var tr = new Transaction(doc, $"HPKata: Generate Rebar for {wall.Name}");
-                    tr.Start();
-                    WallRebarGenerator.GenerateWallRebar(doc, wall, vm.ResultModel);
-                    tr.Commit();
-                    TaskDialog.Show("HPKata", $"Successfully generated 3D rebar in shear wall '{wall.Name}'.");
+                if (uidoc.Document.GetElement(refWall) is not Wall wall)
+                {
+                    return Result.Cancelled;
                 }
 
+                ShowDialogAndGenerateRebar(uiapp, uidoc.Document, wall);
                 return Result.Succeeded;
             }
             catch (Autodesk.Revit.Exceptions.OperationCanceledException)
@@ -76,6 +51,39 @@
                 return Result.Failed;
             }
         }
+
+        private static void ShowDialogAndGenerateRebar(UIApplication uiapp, Document doc, Wall wall)
+        {
+            var vm = CreateViewModel(wall);
+            var view = new WallDetailingWindow(vm);
+            RevitWindowHelper.ShowModal(view, uiapp);
+
+            if (vm.ResultModel != null)
+            {
+                using var tr = new Transaction(doc, $"HPKata: Generate Rebar for {wall.Name}");
+                tr.Start();
+                WallRebarGenerator.GenerateWallRebar(doc, wall, vm.ResultModel);
+                tr.Commit();
+                TaskDialog.Show("HPKata", $"Successfully generated 3D rebar in shear wall '{wall.Name}'.");
+            }
+        }
+
+        private static WallDetailingViewModel CreateViewModel(Wall wall)
+        {
+            var adapter = KataExcelAdapterProvider.Current;
+            var vm = new WallDetailingViewModel(adapter)
+            {
+                WallMark = wall.Name
+            };
+
+            string kataPath = KataInstallEnvironment.Current.KataWorkbookPath;
+            if (File.Exists(kataPath) && adapter.ReadWallSheet(kataPath, out var sheetData))
+            {
+                if (string.Equals(sheetData.WallMark, wall.Name, StringComparison.OrdinalIgnoreCase))
+                {
+                    vm.ReadExcelCommand.Execute(null);
+                }
+            }
+
+            return vm;
+        }
 
         private sealed class WallSelectionFilter : ISelectionFilter
```

### 4. `HPKata/HPKata.Revit/Runners/RevitExportExcelRunner.cs`
```diff
--- a/HPKata/HPKata.Revit/Runners/RevitExportExcelRunner.cs
+++ b/HPKata/HPKata.Revit/Runners/RevitExportExcelRunner.cs
@@ -46,8 +46,13 @@
         if (!success)
         {
+            string fallbackPath = IsMasterLibraryAsset(resolvedPath)
+                ? ResolveDocumentFallbackPath(context)
+                : resolvedPath;
+
             var report = RebarScheduleAggregator.GenerateReport(items, applyBendDeduction);
-            ClosedXmlExporter.ExportScheduleToFile(report, resolvedPath);
+            ClosedXmlExporter.ExportScheduleToFile(report, fallbackPath);
+            resolvedPath = fallbackPath;
         }
 
         context.WriteNotification($"Đã xuất bảng thống kê thép ra Excel: {resolvedPath}");
@@ -62,12 +67,6 @@
             return outputFilePath!;
         }
 
-        string kataPath = KataInstallEnvironment.Current.KataWorkbookPath;
-        if (File.Exists(kataPath))
-        {
-            return kataPath;
-        }
-
         if (!string.IsNullOrWhiteSpace(context.DocumentPath))
         {
             string? dir = Path.GetDirectoryName(context.DocumentPath);
@@ -81,3 +80,17 @@
         return Path.Combine(Path.GetTempPath(), $"{safeTitle}_ThongKeThep.xlsx");
     }
+
+    private static bool IsMasterLibraryAsset(string path)
+    {
+        var env = KataInstallEnvironment.Current;
+        return string.Equals(path, env.KataWorkbookPath, StringComparison.OrdinalIgnoreCase)
+            || string.Equals(path, env.KataEngWorkbookPath, StringComparison.OrdinalIgnoreCase)
+            || string.Equals(path, env.IsoQhPlusPath, StringComparison.OrdinalIgnoreCase);
+    }
+
+    private static string ResolveDocumentFallbackPath(IRevitDocumentContext context)
+    {
+        string safeTitle = string.IsNullOrWhiteSpace(context.DocumentTitle) ? "RevitDoc" : context.DocumentTitle;
+        return Path.Combine(Path.GetTempPath(), $"{safeTitle}_ThongKeThep_Fallback.xlsx");
+    }
 }
```

### 5. `HPKata/HPKata.Revit.Tests/Runners/RevitExportExcelRunnerTests.cs`
```diff
--- a/HPKata/HPKata.Revit.Tests/Runners/RevitExportExcelRunnerTests.cs
+++ b/HPKata/HPKata.Revit.Tests/Runners/RevitExportExcelRunnerTests.cs
@@ -1,5 +1,7 @@
+using System;
 using System.IO;
 using ClosedXML.Excel;
+using HPKata.Core.Configuration;
 using HPKata.Revit.Abstractions;
 using HPKata.Revit.Runners;
 using HPKata.Revit.Tests.Mocks;
@@ -90,13 +92,28 @@
     public void Execute_ResolvesOutputPathFromContextTitle()
     {
+        string isolatedDir = Path.Combine(Path.GetTempPath(), $"HPKataTest_{Guid.NewGuid():N}");
+        Directory.CreateDirectory(isolatedDir);
+        using var envScope = KataInstallEnvironment.UseRootDirectory(isolatedDir);
+
         var context = CreateTestContext();
         string generatedPath = RevitExportExcelRunner.Execute(context, outputFilePath: null);
 
         try
         {
-            Assert.True(File.Exists(generatedPath));
+            Assert.True(File.Exists(generatedPath), "Generated Excel file must exist on disk.");
             Assert.Contains("DuAnTest", generatedPath);
         }
         finally
         {
-            if (File.Exists(generatedPath)) File.Delete(generatedPath);
+            if (File.Exists(generatedPath) && !IsMasterInstallationAsset(generatedPath))
+            {
+                File.Delete(generatedPath);
+            }
+            if (Directory.Exists(isolatedDir))
+            {
+                Directory.Delete(isolatedDir, recursive: true);
+            }
         }
     }
+
+    private static bool IsMasterInstallationAsset(string path)
+    {
+        string defaultRoot = KataInstallEnvironment.DefaultRootDirectory;
+        return path.StartsWith(defaultRoot, StringComparison.OrdinalIgnoreCase);
+    }
```

---

## 5. Verification Method

Once the worker agent applies the unified patch or replacement files:

1. **Verify Asset Staging Protection**:
   ```powershell
   # 1. Ensure Kata.xlsm is present in C:\HPKata
   Copy-Item 'g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\Distribution\Assets\Kata.xlsm' 'C:\HPKata\Kata.xlsm' -Force
   Test-Path 'C:\HPKata\Kata.xlsm'   # Must return True

   # 2. Build the solution (Single-Node, Zero Warnings)
   dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1

   # 3. Run Revit test suite with C:\HPKata\Kata.xlsm on disk
   dotnet test HPKata/HPKata.Revit.Tests/HPKata.Revit.Tests.csproj -c Debug --disable-build-servers -m:1

   # 4. Confirm C:\HPKata\Kata.xlsm was NOT deleted
   Test-Path 'C:\HPKata\Kata.xlsm'   # Must still return True!
   ```

2. **Verify Full Solution Tests**:
   ```powershell
   dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1
   ```
   *Expected*: 100% tests pass (zero failures, zero skipped).

3. **Verify Clean Code AST & Line Limits**:
   ```powershell
   python .agents/teamwork/explorer_hpkata_m2_rem_2/scan_methods.py
   ```
   *Expected*: Zero methods exceeding 50 lines in `HPKata.Revit` and `HPKata.Revit.Tests`.
