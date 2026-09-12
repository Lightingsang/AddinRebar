# Adversarial Challenge Report: M4 ViewModel State & Parameter Validation Engine

**Evaluator**: challenger_m4_1  
**Milestone**: M4 (WPF MVVM UI, Dynamic Theming, and Interactive Preview Canvases)  
**Target Codebase**: `HPRebar/HPRebar/Beam Rebar/View Models/`, `HPRebar/HPRebar/Beam Rebar/View/`  
**Overall Risk Assessment**: **HIGH**  
**Verdict**: **CHALLENGE_FAILED**

---

## 1. Executive Summary

Milestone M4 establishes a comprehensive WPF MVVM presentation layer with dual interactive preview canvases, 5 configuration tabs, dynamic theme tokens, and parameter validation.

The core validation engine in `BeamRebarSession.Validate(out string error)` correctly checks and prevents the primary catastrophic failure modes:
1. Corner bar counts < 2 (0, 1, negative).
2. Non-positive cover ($\le 0$) and dense/sparse stirrup spacings ($\le 0$).
3. Extreme physical clearance violations ($2 \cdot Cover + 2 \cdot \phi_{stirrup} + \phi_{main} \ge \min(b, h)$).
4. Dense stirrup count exceeding Revit's hard limit (> 1002 ties).
5. Validation failures properly abort execution before calling `IBeamRebarRunner.Run` and surface messages to the user via status text and native Revit warning dialogs.

However, adversarial stress-testing identified **two significant defects**, one of which is a **critical two-way binding failure**:
- **Critical Defect (Two-Way Binding Failure)**: `AdditionalBarsTabView.xaml` binds `SelectedItem` to `Session.SelectedSupportEditor` and `Session.SelectedSpanEditor`. Both properties in `BeamRebarSession` are **get-only** (`public ... => ...;`) without setters. WPF cannot write back the selected item when the user interacts with the ComboBox, causing silent data binding errors and permanently locking the active editor to Support 0 / Span 0.
- **Medium Defect (Asymmetric Spacing Validation)**: `Validate()` checks if `LengthClear / StirrupSpacingDense > 1002`, but fails to validate `StirrupSpacingSparse`. If a user enters an accidental typo in midspan spacing (e.g. $s_{sparse} = 5$ mm, $s_{dense} = 150$ mm), `Validate()` passes, but `BeamStirrupDistributionCalculator.ComputeSpanRuns` throws an unhandled `ArgumentOutOfRangeException` during execution.

---

## 2. Adversarial Challenges & Findings

### [Critical] Finding 1: Two-Way Binding Breakdown on Read-Only Selection Properties in `AdditionalBarsTabView`

- **Component**: `HPRebar/HPRebar/Beam Rebar/View/Tabs/AdditionalBarsTabView.xaml` (lines 24, 116) and `HPRebar/HPRebar/Beam Rebar/View Models/BeamRebarSession.cs` (lines 222–226).
- **Assumption Challenged**: Two-way synchronization between tab ViewModels, ComboBox controls, and `BeamRebarSession` selection state.
- **Failure Mode / Attack Scenario**:
  In `AdditionalBarsTabView.xaml`:
  ```xaml
  <!-- Support Selector -->
  <ComboBox ItemsSource="{Binding SupportTopBars}"
            SelectedItem="{Binding Session.SelectedSupportEditor}"
            DisplayMemberPath="SupportName" .../>

  <!-- Span Selector -->
  <ComboBox ItemsSource="{Binding SpanBottomBars}"
            SelectedItem="{Binding Session.SelectedSpanEditor}"
            DisplayMemberPath="SpanName" .../>
  ```
  In `BeamRebarSession.cs`:
  ```csharp
  public SupportTopBarEditor? SelectedSupportEditor =>
      SelectedSupportIndex >= 0 && SelectedSupportIndex < SupportTopBars.Count ? SupportTopBars[SelectedSupportIndex] : null;

  public SpanBottomBarEditor? SelectedSpanEditor =>
      SelectedSpanIndex >= 0 && SelectedSpanIndex < SpanBottomBars.Count ? SpanBottomBars[SelectedSpanIndex] : null;
  ```
  Neither property defines a `set` accessor.
  When the user opens the dropdown and clicks "Support 2" or "Span 2", WPF's default `TwoWay` binding on `ComboBox.SelectedItem` attempts to assign `Session.SelectedSupportEditor = value`.
  Because there is no setter, the assignment fails with `System.Windows.Data Error: 8 : Cannot save value from target back to source`.
  `SelectedSupportIndex` and `SelectedSpanIndex` remain unchanged (at 0).
- **Blast Radius**:
  The user is completely unable to customize additional bars for intermediate or end supports/spans (Support 2, 3, etc.) from the Additional Bars tab. Any parameters entered will erroneously overwrite Support 1 / Span 1.
- **Mitigation**:
  Provide explicit setters in `BeamRebarSession.cs`:
  ```csharp
  public SupportTopBarEditor? SelectedSupportEditor
  {
      get => SelectedSupportIndex >= 0 && SelectedSupportIndex < SupportTopBars.Count ? SupportTopBars[SelectedSupportIndex] : null;
      set
      {
          if (value is not null)
          {
              int idx = SupportTopBars.IndexOf(value);
              if (idx >= 0) SelectedSupportIndex = idx;
          }
      }
  }

  public SpanBottomBarEditor? SelectedSpanEditor
  {
      get => SelectedSpanIndex >= 0 && SelectedSpanIndex < SpanBottomBars.Count ? SpanBottomBars[SelectedSpanIndex] : null;
      set
      {
          if (value is not null)
          {
              int idx = SpanBottomBars.IndexOf(value);
              if (idx >= 0) SelectedSpanIndex = idx;
          }
      }
  }
  ```
  Or change XAML to bind `SelectedIndex="{Binding Session.SelectedSupportIndex}"` and `SelectedIndex="{Binding Session.SelectedSpanIndex}"`.

---

### [Medium] Finding 2: Asymmetric Stirrup Spacing Limit Check (> 1002 Stirrups)

- **Component**: `HPRebar/HPRebar/Beam Rebar/View Models/BeamRebarSession.cs` (lines 404–409).
- **Assumption Challenged**: All user input combinations causing bar counts $> 1002$ are caught prior to execution.
- **Failure Mode / Attack Scenario**:
  In `Validate()`:
  ```csharp
  int estimatedStirrups = (int)Math.Ceiling(span.LengthClear / StirrupSpacingDense) + 1;
  if (estimatedStirrups > 1002)
  {
      errorMessage = $"Span {span.Name}: Dense stirrup spacing produces {estimatedStirrups} ties, exceeding Revit's 1000 limit.";
      return false;
  }
  ```
  `Validate()` only inspects `StirrupSpacingDense`.
  If a user configures `StirrupSpacingDense = 150` mm and `StirrupSpacingSparse = 5` mm (typo):
  `estimatedStirrups = ceil(6000 / 150) + 1 = 41 <= 1002` -> **Validation passes!**
  However, in `BeamStirrupDistributionCalculator.cs` line 35:
  ```csharp
  if ((clearSpanMm / spec.SpacingDense) > MaxBarPositions || (clearSpanMm / spec.SpacingSparse) > MaxBarPositions)
      throw new ArgumentOutOfRangeException(nameof(spec), $"Requested spacing produces bar count exceeding maximum {MaxBarPositions}.");
  ```
  Here `6000 / 5 = 1200 > 1002`, so the domain calculator throws `ArgumentOutOfRangeException`.
- **Blast Radius**:
  The operation fails during execution rather than being rejected cleanly during the pre-run validation phase.
- **Mitigation**:
  Update `Validate()` to check both spacings:
  ```csharp
  double minSpacing = Math.Min(StirrupSpacingDense, StirrupSpacingSparse);
  int estimatedStirrups = (int)Math.Ceiling(span.LengthClear / minSpacing) + 1;
  if (estimatedStirrups > 1002)
  {
      errorMessage = $"Span {span.Name}: Stirrup spacing produces {estimatedStirrups} ties, exceeding Revit's 1000 limit.";
      return false;
  }
  ```

---

### [Low] Finding 3: Lack of Validation on Node Stirrup Spacing & Secondary Tie Spacings

- **Component**: `BeamRebarSession.Validate(out string error)`
- **Assumption Challenged**: All parameters that feed into division operations in domain calculators are validated for positive values.
- **Failure Mode**:
  If `IncludeStirrupsInNodes` is checked and `NodeSpacing <= 0`, `BeamStirrupDistributionCalculator.ComputeNodeRun` throws `ArgumentOutOfRangeException(nameof(spacingMm), "Spacing must be strictly positive.")`.
  Similarly, `CrossTieSpacing` and `MaxVerticalSpacing` are not validated for $> 0$.
- **Mitigation**:
  Add validation in `Validate()`:
  ```csharp
  if (IncludeStirrupsInNodes && NodeSpacing <= 0)
  {
      errorMessage = "Column node stirrup spacing must be greater than zero.";
      return false;
  }
  ```

---

## 3. Stress Test Results Matrix

| Scenario / Edge Case | Input Parameters | Expected Behavior | Actual Behavior | Verdict |
|---|---|---|---|---|
| **EC1.1**: Top bar count = 0 | `TopBarCount = 0`, `BottomBarCount = 2` | Rejected with clear message | `Validate() == false`, error: "Top and bottom main longitudinal reinforcement must each have at least 2 bars." | **PASS** |
| **EC1.2**: Top bar count = 1 | `TopBarCount = 1`, `BottomBarCount = 2` | Rejected with clear message | `Validate() == false`, error matches above | **PASS** |
| **EC1.3**: Bottom bar count = -3 | `TopBarCount = 2`, `BottomBarCount = -3` | Rejected with clear message | `Validate() == false`, error matches above | **PASS** |
| **EC2.1**: Concrete cover = 0 | `Cover = 0`, spacings = 100/200 | Rejected with clear message | `Validate() == false`, error: "Stirrup spacing and concrete cover must be positive values greater than zero." | **PASS** |
| **EC2.2**: Dense spacing = -50 | `StirrupSpacingDense = -50` | Rejected with clear message | `Validate() == false`, error matches above | **PASS** |
| **EC2.3**: Sparse spacing = 0 | `StirrupSpacingSparse = 0` | Rejected with clear message | `Validate() == false`, error matches above | **PASS** |
| **EC3.1**: Beam width = minRequired ($b \le min$) | $b = 86$ mm, $Cover = 25$, $d_{stir} = 8$, $d_{main} = 20$ ($min = 86$) | Rejected as too narrow | `Validate() == false`, error: "Span Span 1: Beam width (86 mm) is too narrow for cover (25 mm) and bar sizes." | **PASS** |
| **EC3.2**: Beam width < minRequired ($b < min$) | $b = 85$ mm, same parameters | Rejected as too narrow | `Validate() == false`, error matches above | **PASS** |
| **EC3.3**: Beam height < minRequired ($h < min$) | $h = 80$ mm, same parameters | Rejected as too shallow | `Validate() == false`, error: "Span Span 1: Beam height (80 mm) is too shallow for cover (25 mm) and bar sizes." | **PASS** |
| **EC3.4**: Null bar type selected | `TopBarType = null` | Rejected before property access | `Validate() == false`, error: "Please ensure main top, bottom, and stirrup rebar types are selected." | **PASS** |
| **EC4.1**: Dense spacing > 1002 ties | $L_n = 6000$ mm, $s_{dense} = 5$ mm (1201 ties) | Rejected as exceeding 1000 limit | `Validate() == false`, error: "Span Span 1: Dense stirrup spacing produces 1201 ties, exceeding Revit's 1000 limit." | **PASS** |
| **EC4.2**: Sparse spacing > 1002 ties | $L_n = 6000$ mm, $s_{dense} = 150$, $s_{sparse} = 5$ | Pre-run validation should catch it | `Validate() == true` (Passes validation!); throws `ArgumentOutOfRangeException` during `Run()` | **FAIL** |
| **EC5.1**: Runner execution prevention | Validation failure triggers | `_runner.Run` is NOT called; Dialog stays open | Execution halts at `return;`; `StatusMessage` set; `RevitDialogs.Warning` displayed | **PASS** |
| **EC6.1**: Two-way binding for Additional Top Bars | User selects "Support 2" in ComboBox | `SelectedSupportEditor` and details switch to Support 2 | Binding fails (property has no setter); remains stuck on Support 1 | **FAIL** |
| **EC6.2**: Two-way binding for Additional Bottom Bars | User selects "Span 2" in ComboBox | `SelectedSpanEditor` and details switch to Span 2 | Binding fails (property has no setter); remains stuck on Span 1 | **FAIL** |
| **EC6.3**: Two-way binding on MainBars & Stirrups | User changes textbox / checkbox | `Session` updates immediately, canvas debounces | Verified: `UpdateSourceTrigger=PropertyChanged` and canvas redraws correctly | **PASS** |

---

## 4. Required Actionable Fixes

To achieve full approval, the following fixes are required:

1. **Fix `SelectedSupportEditor` and `SelectedSpanEditor` setters** in `HPRebar/HPRebar/Beam Rebar/View Models/BeamRebarSession.cs`:
   Implement two-way setters matching the selected item back to `SelectedSupportIndex` and `SelectedSpanIndex`.
2. **Include `StirrupSpacingSparse` in limit validation** in `BeamRebarSession.Validate`:
   ```csharp
   double minSpacing = Math.Min(StirrupSpacingDense, StirrupSpacingSparse);
   int estimatedStirrups = (int)Math.Ceiling(span.LengthClear / minSpacing) + 1;
   if (estimatedStirrups > 1002)
   {
       errorMessage = $"Span {span.Name}: Stirrup spacing produces {estimatedStirrups} ties, exceeding Revit's 1000 limit.";
       return false;
   }
   ```
3. **Validate `NodeSpacing`** when `IncludeStirrupsInNodes` is enabled.
