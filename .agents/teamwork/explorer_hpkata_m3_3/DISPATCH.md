# Dispatch Specification — M3 Visual Assets & Ribbon Icons Explorer (explorer_hpkata_m3_3)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m3_3\`
- Parent Conversation ID: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (lines 1450-1512)
- Master Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md` (Milestone M3, Features 11 & 12)
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`

## Your Mission:
1. Investigate Features 11 & 12: Ribbon PNG Icons & Visual Diagram Binding:
   - Feature 11: Inspect `C:\HPKata\Icon\Ribbon\*.png` (25 icons) and investigate how `HPKata.AutoCad` (CUIX / Ribbon tabs) and `HPKata.Revit` (RibbonPanel buttons) load button images.
     - Architect `KataIconProvider` in `HPKata.UI` or `HPKata.Core` to resolve PNG paths from `KataInstallEnvironment.Current.RibbonIconDirectory`, returning `BitmapImage` with vector fallback if PNG is missing.
   - Feature 12: Inspect shape diagrams in `C:\HPKata\dangthep\` (101 WMF files), column splice diagrams in `C:\HPKata\Cot\` (4 WMF files), and UI images in `C:\HPKata\IMG\` (3 PNG files).
     - Architect image resolution and WPF Image binding in `HPKata.UI` windows (`BeamDetailingWindow`, `ColumnDetailingWindow`, `WallDetailingWindow`, `SlabDetailingWindow`, `FoundationDetailingWindow`, `StairLintelDetailingWindow`, `SetupWindow`).
     - Support WMF rendering or fallback PNG/drawing brush in WPF.
2. STRICT UI CONSTRAINTS:
   - Retain `System.Windows.Window` + `MaterialDesignThemes` v5.2.1. Do NOT switch to `MahApps.Metro`.
   - Retain all `Theme.xaml` `StaticResource` keys.
   - Ensure all 13 WPF windows instantiate on STA thread without throwing under both Dark and Light themes.
3. Clean Code compliance:
   - File length <= 250 lines (Rule C2 <= 300).
   - Method length <= 40 lines (Rule M3 <= 50).
   - 0 unannotated catch blocks (Rule Q-B2).
4. Design unit tests in `HPKata.UI.Tests` and STA E2E tests in `HPKata.E2E.Tests`:
   - Verify icon resolution from `C:\HPKata\Icon\Ribbon\`.
   - Verify shape and splice diagram path resolution and WPF Image source binding.
5. Write your comprehensive report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m3_3\handoff.md` and send a message to parent when done.


## 2026-10-18T05:10:09Z
[Message] timestamp=2026-10-18T05:10:09Z sender=3c323530-716e-4b3b-a14c-2a7fcee0c68a priority=MESSAGE_PRIORITY_HIGH content=You are explorer_hpkata_m3_3 (M3 Visual Assets & Ribbon Icons Explorer).
Your Working Directory is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m3_3\
Your Dispatch Specification is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\explorer_hpkata_m3_3\DISPATCH.md
The Authoritative User Request is: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md (lines 1450-1512).
Master Scope Document: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md (Milestone M3, Features 11 & 12).
Target Solution: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx
Your Parent Conversation ID is: 3c323530-716e-4b3b-a14c-2a7fcee0c68a
