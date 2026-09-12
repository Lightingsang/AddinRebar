# Handoff Report: Views, Dimensions, Annotations, Orchestration & Command
## Milestone M3 Part 3 — Continuous Beam Rebar

- **Agent**: `explorer_m3_3`
- **Working Directory**: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m3_3`
- **Output Specification**: `views_orch_plan.md`
- **Target Location**: `HPRebar/HPRebar/Beam Rebar/`
- **Date**: 2026-09-07

---

## 1. Observation

### 1.1 Golden Reference Implementations
Direct inspection of `HPRebar/HPRebar/Column Rebar/` established the following architectural patterns:
1. **Longitudinal Elevation Views (`DetailViewCreator.cs:19-44, 51-65, 87-103`)**:
   - Uses `ViewSection.CreateSection` / `ViewSection.CreateDetail`.
   - View family type resolution: dynamically checks `ViewFamilyType` where `ViewFamily == ViewFamily.Section` (or `ViewFamily.Detail`), creating `@ColumnDetail` if not found via `template?.Duplicate(name)`.
   - Sets `view.get_Parameter(BuiltInParameter.VIEWER_CROP_REGION_VISIBLE)?.Set(0)` to hide crop boundaries.
   - Robust naming with fallback suffixing: `Rename(view, name)` catches `ArgumentException` on name collisions and renames to `{name}A`.
2. **Transverse Cross-Section Views (`SectionViewCreator.cs:20-45, 84-108`)**:
   - Cuts section at specific stations along the member.
   - Sets asymmetric crop box with extra margin on right:
     `Min = new XYZ(-width / 2 - margin / 2, -depth / 2 - margin / 2, 0)`
     `Max = new XYZ(width / 2 + 2.5 * margin, depth / 2 + margin / 2, margin / 2)`
     reserving space for the tabular rebar schedule.
3. **Automated Dimensioning & Stable Reference Conversion (`DimensionCreator.cs:85-127`)**:
   - Verbatim code at lines 121-127:
     ```csharp
     private static Reference ToLinearReference(Document document, PlanarFace face)
     {
         var surface = face.Reference.ConvertToStableRepresentation(document);
         var linear = surface.Replace("SURFACE", "LINEAR");
         return Reference.ParseFromStableRepresentation(document, linear);
     }
     ```
   - Every dimension creation is guarded with a `try/catch` block: failure logs a warning and returns 0, never aborting the reinforcement workflow.
4. **Schedule Tables & Tagging (`RebarTableTagCreator.cs:27-68, 101-134`)**:
   - Draws tabular annotations directly in the section view using `document.Create.NewDetailCurve` and `TextNote.Create`.
   - Scales row height dynamically with text size and view scale:
     `RowHeight = scale > 0 ? height * (100.0 / scale) : height`.
5. **Atomic Orchestration (`ColumnRebarOrchestrator.cs:58-92`)**:
   - Verbatim code at lines 58-61, 77, 86-90:
     ```csharp
     using var group = new TransactionGroup(_document, "Column Rebar");
     group.Start();
     try
     {
         // views -> dimensions -> rebar -> tables
         group.Assimilate();
     }
     catch (Exception exception)
     {
         Log.Error(exception, "Column Rebar failed; the model was rolled back");
         group.RollBack();
         throw;
     }
     ```
   - Each sub-step runs in its own named `Transaction` with `RebarFailureHandling.Apply(transaction)` attached immediately after `transaction.Start()`.
6. **Decoupled Runner (`RevitRebarRunner.cs:12-33`)**:
   - Implements `IColumnRebarRunner`, cleanly isolating the WPF ViewModel from direct Revit API types.
7. **ExternalCommand Entry Point (`ColumnRebarCommand.cs:24-108`)**:
   - Uses `[UsedImplicitly]` and `[Transaction(TransactionMode.Manual)]`.
   - Inherits `Application` (which is `UIApplication` from `Nice3point.Revit.Toolkit.External.ExternalCommand`).
   - Handles `Autodesk.Revit.Exceptions.OperationCanceledException` cleanly on user escape.
   - Sets window owner: `new WindowInteropHelper(view).Owner = Application.MainWindowHandle`.
   - Applies theme: `ThemeSwitcher.ApplyFromRevit(view)`.
8. **Supporting Infrastructure**:
   - `RebarFailureHandling.cs:15-49`: `SwallowWarnings : IFailuresPreprocessor` deletes warnings, logs them via `Log.Warning`, and preserves errors.
   - `RevitUnits.cs:9-23`: `UnitUtils.ConvertToInternalUnits(mm, UnitTypeId.Millimeters)` with zero deprecated unit APIs.
   - `ThemeSwitcher.cs:70-77`: Evaluates `Autodesk.Revit.UI.UIThemeManager.CurrentTheme` guarded with `#if REVIT2024_OR_GREATER`.
   - `RevitDialogs.cs:9-61`: Native TaskDialog wrappers without `System.Windows.Forms`.

---

## 2. Logic Chain

1. **Geometry Orientation Mapping**:
   - Unlike columns which are oriented along the vertical $Z$ axis, a continuous beam is a horizontal 1D chain oriented along its longitudinal axis $\vec{X}_{beam}$ with transverse vector $\vec{Y}_{beam} = \vec{Z} \times \vec{X}_{beam}$.
   - Elevation detail views must cut along $\vec{X}_{beam}$ and vertical $\vec{Z}$, with the camera looking in direction $\vec{Y}_{beam}$.
   - Transverse cross-sections must cut across $\vec{Y}_{beam}$ and $\vec{Z}$, with the camera looking along $\vec{X}_{beam}$.
2. **Dimension Reference Transformation**:
   - In Revit API, `ViewSection` rejects `SURFACE` references for dimensions.
   - Planar faces of structural framing instances only provide `SURFACE` references via `PlanarFace.Reference`.
   - Rewriting the stable representation token `SURFACE` to `LINEAR` provides a valid reference representing the face boundary edge in section, proven by the Golden Reference `DimensionCreator.cs:121-127`.
   - All dimension operations must be wrapped in `try/catch` so dimension failure never invalidates generated rebar.
3. **Transaction Safety**:
   - Continuous beam generation produces dozens of Revit elements (views, dimensions, stirrup sets, longitudinal bars, additional bars, side bars, hanging ties, schedule lines, text notes).
   - A single outer `TransactionGroup("Beam Rebar")` guarantees that any failure or user cancellation cleanly restores the model to its initial state via `group.RollBack()`.
   - Successful runs merge all sub-transactions into a single undo entry via `group.Assimilate()`.
4. **Warning Suppression**:
   - Revit inevitably emits non-fatal warnings for touching rebar curves or bars slightly intersecting host cover.
   - Without an `IFailuresPreprocessor`, Revit displays modal warning dialogs that block headless/automated execution. Attaching `SwallowWarnings` clears warnings while allowing true errors to trigger rollback.
5. **WPF MVVM Decoupling**:
   - Defining `IBeamRebarRunner` enables `BeamRebarViewModel` to remain completely free of Revit API references (`Document`, `TransactionGroup`), enabling independent unit testing and design-time preview.

---

## 3. Caveats

1. **Stable Representation Rewriting**:
   - The string substitution `SURFACE` -> `LINEAR` relies on undocumented Revit stable representation syntax. Although verified across Revit 2022 through 2026, future Revit major versions could change token formats. All callers must wrap dimensioning in `try/catch` and log warnings on failure.
2. **Template Availability**:
   - If a host project has no section or detail view templates, `DetailViewCreator` and `SectionViewCreator` duplicate the default section family type. If no section family types exist at all in the document, view creation is skipped with a logged warning.
3. **Far-Clip Plane Limits**:
   - For transverse cross-sections, the section box depth must remain tight ($\pm \text{margin} / 2$) to prevent background framing elements from cluttering the cross-section drawing.

---

## 4. Conclusion

A comprehensive, fully specified architectural plan for Milestone M3 Part 3 has been designed and authored in:
`f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\explorer_m3_3\views_orch_plan.md`

The plan provides complete C# class structures, exact method signatures, geometric transforms, error containment mechanisms, and multi-version preprocessor directives for:
1. `HPRebar/HPRebar/Beam Rebar/DetailViewCreator.cs`
2. `HPRebar/HPRebar/Beam Rebar/SectionViewCreator.cs`
3. `HPRebar/HPRebar/Beam Rebar/DimensionCreator.cs`
4. `HPRebar/HPRebar/Beam Rebar/RebarTableTagCreator.cs`
5. `HPRebar/HPRebar/Beam Rebar/BeamRebarOrchestrator.cs`
6. `HPRebar/HPRebar/Beam Rebar/RevitRebarRunner.cs`
7. `HPRebar/HPRebar/Beam Rebar/BeamRebarCommand.cs`
8. Supporting classes: `RebarFailureHandling.cs`, `RevitUnits.cs`, `LocalizationService.cs`, `ThemeSwitcher.cs`, `RevitDialogs.cs`
9. Associated data models: `CreatedBeamViews.cs`, `CreatedBeamRebar.cs`, `BeamOrchestratorResult.cs`, `BeamAnnotationSettings.cs`

---

## 5. Verification Method

### 5.1 Independent Compilation Verification
Once implemented by the developer agent, the solution must build without warnings or errors across the target Revit configurations:
```bash
dotnet build HPRebar/HPRebar.slnx -c Debug.R25 -p:DeployAddin=false
dotnet build HPRebar/HPRebar.slnx -c Debug.R26 -p:DeployAddin=false
```

### 5.2 Deprecated API Grep Check
Verify zero obsolete Revit API usages:
- Verify `DisplayUnitType` is nowhere in the codebase.
- Verify `UnitType.` is nowhere in the codebase.
- Verify `ElementId.IntegerValue` is guarded by `#if !REVIT2024_OR_GREATER`.

### 5.3 Invalidation Conditions
The design is invalidated if:
1. `NewDimension` in `ViewSection` throws unhandled exceptions during execution.
2. `TransactionGroup` fails to rollback on error or fails to assimilate on success.
3. The add-in shows modal Revit warning popups during rebar generation.
4. `HPRebar.Core` references any `Autodesk.Revit.*` namespaces.
