# Dispatch Specification — M2 Iteration 2 Empirical Challenger 2 (challenger_hpkata_m2_rem_2)

- Working Directory: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_hpkata_m2_rem_2\`
- Parent Conversation ID: `3c323530-716e-4b3b-a14c-2a7fcee0c68a`
- Authoritative User Request: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (lines 1450-1512)
- Master Scope Document: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\PROJECT.md` (Milestone M2, Features 6–8)
- Target Solution: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\HPKata\HPKata.slnx`
- Worker Handoff: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_hpkata_m2_rem\handoff.md`

## Your Mission:
1. Empirically challenge UI Window default instantiation & theme switching:
   - Run `HPKata.E2E.Tests` targeting `Tier1_M2_ChallengerUiStressTests` on STA thread to verify all 13 WPF windows instantiate cleanly under both Dark and Light themes using parameterless constructors.
   - Verify that no window constructor throws CS7036 or runtime exceptions.
2. Empirically verify method lengths in production code:
   - Check `HPKata.Revit/Commands/ColumnRebarCommands.cs`, `FramingRebarCommands.cs`, `WallRebarCommands.cs` (Execute <= 35 lines).
   - Check `HPKata.Core/Excel/Mappers/KataColumnDataMapper.cs` (ToRebarLayout <= 25 lines).
   - Verify that 0 production methods in `HPKata.Core`, `HPKata.UI`, `HPKata.AutoCad`, `HPKata.Revit`, and `HPKata.Installer` exceed 50 lines.
3. Formulate your empirical verdict: **APPROVE** or **REJECT**.
4. Write your report to `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\challenger_hpkata_m2_rem_2\handoff.md` and send a message to parent when done.
