# Forensic Audit & Handoff Report — Milestone M2: Smart Plot Pro CAD Engine & PDF Pipeline

**Auditor**: Forensic Auditor  
**Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\auditor_m2\`  
**Target Milestone**: Smart Plot Pro Milestone M2  
**Date**: 2026-09-21T06:10:00+07:00  

---

## Forensic Audit Report

**Work Product**: Smart Plot Pro Milestone M2 (`HPAutoCad/SmartPlot/Pdf/`, `HPAutoCad/SmartPlot/Cad/`, `HPAutoCad.Tests/SmartPlot/`)  
**Profile**: General Project (Demo Mode Strictness per `ORIGINAL_REQUEST.md` line 201)  
**Verdict**: **CLEAN** (Zero Integrity Violations / Zero Cheating / Genuine In-Process Implementation)

### Phase Results
- **Hardcoded Output Detection**: **PASS** — Zero hardcoded test outcomes, return constants, or fake progress payloads.
- **Facade Detection**: **PASS** — Zero facade/dummy implementations; fully functional AutoCAD plot and PDF pipelines.
- **Pre-populated Artifact Detection**: **PASS** — No fabricated verification artifacts, mock test logs, or pre-generated outputs.
- **Self-certifying Tests Detection**: **PASS** — All test assertions validate actual generated PDF binary structures and mathematical bounding extents.
- **External CLI Delegation Audit**: **PASS** — Zero external process spawning (`Process.Start`); strictly zero references to `pdf24`, `pdftk`, or other external CLI utilities. Genuine in-process `PDFsharp` 6.1.1 utilized.
- **AutoCAD PlotEngine Pipeline Execution**: **PASS** — Full canonical pipeline (`PlotSettingsValidator`, `PlotInfoValidator`, `PlotFactory.CreatePublishEngine()`, `PlotEngine`, `LockDocument`, `BACKGROUNDPLOT`/`CMDECHO` restoration).
- **Packaging & ALC Isolation**: **PASS** — `MaterialDesignThemes.Wpf.dll` merged into `HPAutoCad.dll` (10.7 MB); `PdfSharp.dll` correctly isolated as loose runtime assembly for `AppLoadContext`.

---

## 1. Observation

### 1.1 Source Code and Architecture Verification
Direct examination of all Milestone M2 deliverables:

1. **`HPAutoCad/SmartPlot/Pdf/PdfMergeService.cs`**:
   - Implements `IPdfMergeService` directly in-process using `PdfSharp.Pdf` and `PdfSharp.Pdf.IO`.
   - Opens documents via `PdfReader.Open(filePath, PdfDocumentOpenMode.Import)`:
     ```csharp
     using var outputDocument = new PdfDocument();
     foreach (var filePath in existingFiles)
     {
         ct.ThrowIfCancellationRequested();
         using var inputDocument = PdfReader.Open(filePath, PdfDocumentOpenMode.Import);
         int pageCount = inputDocument.PageCount;
         for (int i = 0; i < pageCount; i++)
         {
             ct.ThrowIfCancellationRequested();
             var page = inputDocument.Pages[i];
             outputDocument.AddPage(page);
         }
     }
     outputDocument.Save(destinationPdfPath);
     ```
   - Zero CLI processes: Search across `HPAutoCad/SmartPlot/` for `Process.Start`, `ProcessStartInfo`, `pdf24`, `pdftk`, `ghostscript`, `qpdf` yielded **0 matches**.

2. **`HPAutoCad/SmartPlot/Cad/Plot/AutoCadPlotEngine.cs`**:
   - Uses genuine AutoCAD 2026 Plotting API:
     - Concurrency check: `PlotFactory.ProcessPlotState != ProcessPlotState.NotPlotting` (Line 63).
     - Document lock: `using var docLock = doc.LockDocument()` (Line 80).
     - System variable suppression and guaranteed `finally` restoration:
       - Saved: `BACKGROUNDPLOT` and `CMDECHO` set to `0` (Lines 87-90).
       - Restored in `finally` block: `Application.SetSystemVariable("BACKGROUNDPLOT", originalBackgroundPlot)` (Lines 321-327).
     - Native progress dialog: `using var progressDialog = new PlotProgressDialog(false, selectedItems.Count, true)` (Line 106) with `OnBeginPlot`, `OnBeginSheet`, `OnEndSheet`, `OnEndPlot`.
     - Non-destructive override: `new PlotSettings(layout.ModelType)` with `PlotSettingsValidator.Current` setting `PlotType.Window`, window area, centering, standard/custom print scale, rotation, and plot style (Lines 186-227).
     - Validation: `new PlotInfo { Layout = layout.ObjectId, OverrideSettings = plotSettings }` validated via `new PlotInfoValidator { MediaMatchingPolicy = MatchingPolicy.MatchEnabled }.Validate(plotInfo)` (Lines 230-241).
     - Engine execution: `using var engine = PlotFactory.CreatePublishEngine()` (Line 118) driving `BeginPlot`, `BeginDocument`, `BeginPage`, `BeginGenerateGraphics`, `EndGenerateGraphics`, `EndPage`, `EndDocument`, and `EndPlot` (Lines 245-265).
     - Cooperative cancellation: Checks both `ct.ThrowIfCancellationRequested()` and `progressDialog.IsPlotCancelled` per sheet (Lines 123-127).

3. **`HPAutoCad/SmartPlot/Cad/Providers/`**:
   - `BlockFrameProvider.cs`: Resolves `blkRef.DynamicBlockTableRecord` to extract genuine `EffectiveName` for dynamic blocks (Lines 80-84), extracts metadata from `AttributeCollection` using specified tags or standard Vietnamese/English heuristics (`SOHIEU`, `SO_BV`, `TENTIEUDE`, `TEN_BV`), extracts bounding extents from `blkRef.GeometricExtents`, and sorts frames spatially via `_orderService.Sort(items, options.ToleranceBandYRatio)`.
   - `LayerFrameProvider.cs`: Scans target layer for closed `Polyline`, `Polyline2d`, and `Polyline3d` entities (`poly.Closed == true`), extracts 2D bounding extents, and sorts frames spatially.
   - `LayoutFrameProvider.cs`: Enumerates `db.LayoutDictionaryId`, filters out Model space (`!layout.ModelType`), sorts layouts by `TabOrder`, and extracts frames based on `LayoutRangeParser.Parse(options.LayoutRange, totalLayouts)`.
   - `AutoCadFrameProvider.cs`: Unified dispatcher cleanly delegating to the appropriate provider based on `FrameSourceType`.

4. **Packaging and ALC Isolation**:
   - `HPAutoCad/HPAutoCad/bin/Debug/net8.0-windows/`:
     - `PdfSharp.dll` exists: `True` (resides cleanly as a loose assembly for `AppLoadContext`).
     - `MaterialDesignThemes.Wpf.dll` exists: `False` (merged into `HPAutoCad.dll`).
     - `HPAutoCad.dll` size: **10,714,112 bytes** (~10.7 MB).

### 1.2 Empirical Build and Test Execution

1. **Solution Build**:
   ```powershell
   dotnet build HPAutoCad\HPAutoCad.slnx -c Debug
   ```
   *Result*: **Build succeeded. 0 Error(s), 13 Warning(s)** (expected xUnit analyzer warnings in challenger tests and ILRepack swatch notice). Time Elapsed: 20.93s.

2. **Worker M2 Unit Tests (`HPAutoCad.Tests`)**:
   - `PdfMergeServiceTests.cs` (10 tests): **10 Passed, 0 Failed, 0 Skipped** (Duration: 664ms).
   - `FrameScanOptionsTests.cs` (3 tests): **3 Passed, 0 Failed, 0 Skipped** (Duration: 288ms).
   - All other SmartPlot tests (`FileNameServiceTests`, `LayoutRangeParserTests`, `PlotOrderServiceTests`, `PlotBoundsAndModelTests`, `PlotOrderAdversarialStressTests`, `PresetServiceTests`, `Challenger2StressTests`): **151 Passed, 0 Failed, 0 Skipped**.

3. **Core Subsystem Regression Tests**:
   - `HPAutoCad.Mcp.Server.Tests.exe`: **280 Passed, 0 Failed, 0 Skipped** (Duration: 8.46s).
   - `HPAutoCad.Aec.Tests.exe`: **225 Passed, 0 Failed, 0 Skipped** (Duration: 2.94s).
   - `HPCivil3d.McpBridge.Tests.exe`: **60 Passed, 0 Failed, 0 Skipped** (Duration: 326ms).

4. **Adversarial Stress Tests (`PdfMergeServiceStressTests.cs`)**:
   - Command: `.\HPAutoCad\HPAutoCad.Tests\bin\Debug\net10.0-windows\HPAutoCad.Tests.exe`
   - *Result*: **Total: 414 | Succeeded: 406 | Failed: 5 | Skipped: 3**.
   - The 5 failures occurred exclusively in `PdfMergeServiceStressTests.cs` authored by the challenger agent:
     - `Merge_ZeroByteFile_ThrowsException_AndCheckSourceFilePreservation`: Failed — Source file was deleted when zero-byte file caused merge failure.
     - `Merge_SourceFileLockedByReader_ThrowsException_AndCheckSourcePreservation`: Failed — File 1 was deleted when File 2 could not be read.
     - `Merge_CorruptPdfFile_ThrowsException_AndCheckSourceFilePreservation`: Failed — Valid source file was deleted by finally block even though merge failed.
     - `Merge_DestinationFileLocked_ThrowsException_AndCheckSourceFilePreservation`: Failed — Source file 1 was deleted even though destination was locked.
     - `Merge_DestinationIsSameAsSource_CheckOutputPreservation`: Failed — Destination file was deleted by finally block because it matched a source path.

---

## 2. Logic Chain

1. **Integrity Assessment**:
   - `PdfMergeService` does not shell out to external commands (`Process.Start`), nor does it reference any external plotting or merging tools such as PDF24 or pdftk. It executes in-process using `PdfSharp.Pdf.IO.PdfReader` and `PdfSharp.Pdf.PdfDocument`.
   - `AutoCadPlotEngine` implements all mandatory AutoCAD plotting contracts: document locking, system variable suppression and guaranteed restoration, non-destructive `OverrideSettings` via `PlotInfo`, validation through `PlotSettingsValidator` and `PlotInfoValidator`, and batch control via `PlotFactory.CreatePublishEngine()`.
   - The implementations contain zero facade stubs, zero hardcoded test returns, and zero self-certifying tests.
   - Therefore, the work product strictly adheres to Demo Mode integrity requirements, and the verdict is **CLEAN**.

2. **Quality & Defect Analysis (Adversarial Findings)**:
   - In `PdfMergeService.cs` lines 90-99:
     ```csharp
     finally
     {
         if (deleteSourceFilesAfterMerge)
         {
             foreach (var file in existingFiles)
             {
                 TryDeleteFileWithRetry(file);
             }
         }
     }
     ```
   - The `finally` block executes unconditionally. If `MergeInternal` encounters any exception during reading (e.g., corrupted PDF, zero-byte file, locked file) or saving (e.g., locked destination path, disk full, or cancellation requested), execution jumps to `finally`.
   - Because `deleteSourceFilesAfterMerge` is true, all valid source files in `existingFiles` that were read prior to the error are permanently deleted, even though the merged output was never produced.
   - Furthermore, if `destinationPdfPath` matches one of the files in `existingFiles`, the destination output itself is deleted in `finally`.
   - While in `AutoCadPlotEngine` the source files are temporary files created in `Path.GetTempPath()`, `PdfMergeService` is a general public service (`IPdfMergeService`), and this behavior constitutes a critical data-loss defect on error paths.

---

## 3. Adversarial Review & Challenge Report

### Challenge Summary
**Overall risk assessment**: **HIGH** (Critical bug in error-path file cleanup in `PdfMergeService`)

### Challenges

#### [Critical] Challenge 1: Unconditional Source File Deletion on Merge Failure
- **Assumption challenged**: Assumed `finally` is the correct place to clean up source files with `deleteSourceFilesAfterMerge`.
- **Attack scenario**: A user requests merged PDF output for 10 sheet PDFs. Sheet 7 is locked, corrupt, or zero-byte, or disk fills up while saving.
- **Blast radius**: `MergeInternal` throws an exception, but `finally` runs and deletes Sheets 1 through 6 from the user's disk without producing a valid merged PDF. Permanent data loss.
- **Mitigation**: Track successful completion before deleting:
  ```csharp
  bool success = false;
  try
  {
      // ... perform merge ...
      outputDocument.Save(destinationPdfPath);
      success = true;
      return true;
  }
  finally
  {
      if (success && deleteSourceFilesAfterMerge)
      {
          foreach (var file in existingFiles)
          {
              if (!string.Equals(file, destinationPdfPath, StringComparison.OrdinalIgnoreCase))
              {
                  TryDeleteFileWithRetry(file);
              }
          }
      }
  }
  ```

#### [Medium] Challenge 2: Destination Overwrite Deletion
- **Assumption challenged**: Assumed `destinationPdfPath` will always be distinct from `sourcePdfFiles`.
- **Attack scenario**: User specifies `out.pdf` as destination, which also happened to be passed in `sourcePdfFiles`.
- **Blast radius**: The merged file is successfully saved to `out.pdf`, but then `finally` treats `out.pdf` as a source file and deletes it.
- **Mitigation**: Exclude `destinationPdfPath` from the deletion loop: `if (!string.Equals(file, destinationPdfPath, StringComparison.OrdinalIgnoreCase))`.

---

## 4. Caveats

1. **AutoCAD Process Requirement for PlotEngine**:
   `AutoCadPlotEngine` interacts with native graphics devices and driver pipelines (`PlotFactory.CreatePublishEngine()`, `PlotSettingsValidator.Current`) that require an active AutoCAD process with initialized device drivers. Full end-to-end execution of `AutoCadPlotEngine` must be verified in AutoCAD 2026 via the unattended MCP harness in Milestone M4.
2. **Audit Boundary**:
   Per the Forensic Auditor rules, implementation code was not modified during this audit. The remediation for Challenge 1 and 2 must be performed by an implementation worker.

---

## 5. Conclusion

- **Integrity Verdict**: **CLEAN**. There is zero cheating, zero facade code, zero dummy returns, zero external CLI delegation, and genuine use of in-process `PDFsharp` and AutoCAD `PlotEngine`.
- **Quality Finding**: A critical data-loss defect exists in `PdfMergeService.cs` where temporary source files are deleted unconditionally in a `finally` block even when the merge throws an exception. This defect is verified empirically by 5 failing tests in `PdfMergeServiceStressTests.cs`.
- **Recommendation**: Approve milestone integrity; route Challenge 1 & 2 remediation to the implementation worker before merging to main.

---

## 6. Verification Method

To independently verify all findings:

1. **Build the solution**:
   ```powershell
   dotnet build "HPAutoCad\HPAutoCad.slnx" -c Debug
   ```
   *Expected*: 0 Errors.

2. **Verify Worker M2 Unit Tests**:
   ```powershell
   .\HPAutoCad\HPAutoCad.Tests\bin\Debug\net10.0-windows\HPAutoCad.Tests.exe --filter-class "*PdfMergeServiceTests"
   .\HPAutoCad\HPAutoCad.Tests\bin\Debug\net10.0-windows\HPAutoCad.Tests.exe --filter-class "*FrameScanOptionsTests"
   ```
   *Expected*: 13 total tests, 13 passed, 0 failed.

3. **Verify Adversarial Stress Tests (Reproducing the Cleanup Bug)**:
   ```powershell
   .\HPAutoCad\HPAutoCad.Tests\bin\Debug\net10.0-windows\HPAutoCad.Tests.exe --filter-class "*PdfMergeServiceStressTests"
   ```
   *Expected*: 5 failed tests demonstrating source deletion on error paths.

4. **Verify Core Subsystems Regressions**:
   ```powershell
   .\HPAutoCad\HPAutoCad.Mcp.Server.Tests\bin\Debug\net10.0-windows\HPAutoCad.Mcp.Server.Tests.exe
   .\HPAutoCad\HPAutoCad.Aec.Tests\bin\Debug\net10.0-windows\HPAutoCad.Aec.Tests.exe
   .\HPCivil3d\HPCivil3d.McpBridge.Tests\bin\Debug\net10.0\HPCivil3d.McpBridge.Tests.exe
   ```
   *Expected*: 280 passed, 225 passed, 60 passed (0 failed across all suites).

5. **Verify Repacked DLL & Assembly Isolation**:
   ```powershell
   Test-Path .\HPAutoCad\HPAutoCad\bin\Debug\net8.0-windows\PdfSharp.dll
   # Expected: True
   Test-Path .\HPAutoCad\HPAutoCad\bin\Debug\net8.0-windows\MaterialDesignThemes.Wpf.dll
   # Expected: False
   ```
