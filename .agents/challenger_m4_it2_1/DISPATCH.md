# DISPATCH — challenger_m4_it2_1

## Mission
You are Challenger 1 for Milestone M4 Iteration 2 (Parameter Validation Engine & Two-Way Binding Verification).

## Mandatory Reading
1. Authoritative User Request: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md`
2. Master Project Plan: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\PROJECT.md`
3. Worker Handoff: `f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m4_it2\handoff.md`
4. Codebase under test:
   - `HPRebar/HPRebar/Beam Rebar/View Models/BeamRebarSession.cs`
   - `HPRebar/HPRebar/Beam Rebar/View/Tabs/AdditionalBarsTabView.xaml`

## Stress-Test Challenges
1. **Stirrup Spacing Asymmetric Limit Challenge**:
   - Test `Validate(out string err)` when `StirrupSpacingSparse` is set to invalid values:
     - Non-positive value (e.g., 0, -50 mm).
     - Very small value (e.g., 2 mm) that causes bar count $> 1002$.
   - Confirm validation returns `false` with a clear user-facing error message instead of letting Revit API crash.
2. **Node Stirrup Validation Challenge**:
   - Test `Validate` when `IncludeStirrupsInNodes = true` but `NodeSpacing <= 0`.
   - Confirm validation returns `false` with an appropriate error message.
3. **Two-Way Binding & Dropdown Selection Challenge**:
   - Inspect and trace `SelectedSupportEditor` and `SelectedSpanEditor` setters.
   - Confirm that setting `SelectedSupportEditor` or changing ComboBox selection updates `SelectedSupportIndex` and propagates notifications so that dependent sub-controls update immediately.
4. **General Validation Robustness**:
   - Challenge negative cover, main bar count $< 2$, and dimensional clearance violation ($2 \times \text{Cover} + 2 \times \text{stirrup\_dia} + \text{main\_dia} \ge \min(b, h)$).

## Output
Write your challenge report to:
`f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\challenger_m4_it2_1\challenge_report.md`
Write your handoff to:
`f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\challenger_m4_it2_1\handoff.md`
Notify orchestrator via `send_message` with your verdict: **APPROVE** or **CHALLENGE_FAILED**.

## 2026-09-07T10:05:47Z
You are challenger_m4_it2_1.
Your working directory is: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\challenger_m4_it2_1
Read your task assignment at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\challenger_m4_it2_1\DISPATCH.md
Read the authoritative user request at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\ORIGINAL_REQUEST.md
Read the worker handoff at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\worker_m4_it2\handoff.md
Read the target codebase at: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\HPRebar\HPRebar\Beam Rebar\

Stress-test and challenge the ViewModel state and parameter validation engine:
- Challenge BeamRebarSession.Validate: test non-positive and very small StirrupSpacingSparse (bar count > 1002); verify user error message.
- Challenge Node Stirrup validation when IncludeStirrupsInNodes = true and NodeSpacing <= 0.
- Challenge SelectedSupportEditor and SelectedSpanEditor two-way binding synchronization and dropdown selection switching.
- Verify clearance violation checks and bar count < 2 validation.

Write your challenge report to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\challenger_m4_it2_1\challenge_report.md
Write your handoff to: f:\1-CONG VIEC\05-AI\01_Revit\02_Csharp\AddinRebar\.agents\challenger_m4_it2_1\handoff.md
Notify orchestrator via send_message with your verdict (APPROVE or CHALLENGE_FAILED).

