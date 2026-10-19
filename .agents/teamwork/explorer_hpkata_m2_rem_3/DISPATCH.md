# Dispatch Specification — M2 Iteration 2 Explorer (explorer_hpkata_m2_rem_3)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_rem_3\`
- Parent Conversation ID: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (lines 1450-1512)
- Master Scope: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md` (Milestone M2, Features 5–8)
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`

## REVIEWER 2 AND CHALLENGER 2 FINDINGS
1. Reviewer 2 found CS7036 in `HPKata.E2E.Tests/Tier1_M2_ChallengerUiStressTests.cs` because `FoundationDetailingWindow`, `SlabDetailingWindow`, and `StairLintelDetailingWindow` in `HPKata.UI/Views/` were missing default parameterless constructors:
   `public Window() : this(new ViewModel()) { }`
2. Challenger 2 added tests in `HPKata.E2E.Tests` that expect all 13 windows to instantiate via parameterless construction on STA thread.

## Mission:
1. Inspect `FoundationDetailingWindow.xaml.cs`, `SlabDetailingWindow.xaml.cs`, and `StairLintelDetailingWindow.xaml.cs` in `HPKata.UI/Views/`.
2. Design the addition of default parameterless constructors forwarding to their respective viewmodels:
   - `public FoundationDetailingWindow() : this(new FoundationDetailingViewModel()) { }`
   - `public SlabDetailingWindow() : this(new SlabDetailingViewModel()) { }`
   - `public StairLintelDetailingWindow() : this(new StairLintelDetailingViewModel()) { }`
3. Verify that all 13 WPF windows in `HPKata.UI/Views/` have parameterless constructors and instantiate cleanly on STA thread under both Dark and Light themes.
4. Verify that all `.cs` files are <= 300 lines (Rule C2), all methods are <= 50 lines (Rule M3).
5. Write your report to: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m2_rem_3\handoff.md`.
Message parent when done.
