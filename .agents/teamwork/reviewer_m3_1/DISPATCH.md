# Task Dispatch — reviewer_m3_1

## 2026-10-17T11:10:00Z

- **Agent Name**: `reviewer_m3_1`
- **Role**: Milestone M3 Code Architecture & Clean Code Reviewer
- **Working Directory**: `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\reviewer_m3_1\`
- **Parent Conversation ID**: `a7a2dbbb-6b7d-4279-98cd-1216d8280d80`

### Mandatory First Steps:
1. Read the authoritative original request at:
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md` (headers `## 2026-10-16T23:55:10Z` and `## 2026-10-17T06:46:41Z`)
2. Read the master project scope:
   `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_17\PROJECT.md`
3. Read the worker's changes and handoff:
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation_slab_stair\changes.md`
   - `g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\worker_m3_foundation_slab_stair\handoff.md`

### Your Mission:
Review the Milestone M3 implementation focusing on Code Architecture & Clean Code Governance:
1. **Layer & Host Isolation**:
   - Verify `HPKata.Core` has ZERO references to `Autodesk.AutoCAD.*` or `Autodesk.Revit.*`.
   - Verify all Foundation, Slab, Stair, and Lintel models, calculators, and serializers reside purely in `HPKata.Core`.
   - Verify `HPKata.UI` cleanly follows MVVM with `CommunityToolkit.Mvvm` and dynamic binding to `Theme.xaml`.
2. **Clean Code Rules Audit**:
   - Rule C2: Measure every C# file added or touched in Phase 4 — must be $\le 300$ physical lines.
   - Rule M3: Measure methods in Phase 4 code — must be $\le 50$ physical lines.
   - Rule Q-B1: Zero dead/commented-out code, zero unaddressed TODOs/FIXMEs/HACKs/NotImplementedException.
   - Rule Q-B2: Zero unannotated empty catch blocks.
   - Rule R5: Constant `304.8` must appear strictly and exclusively in `RevitUnitConverter.cs`.
3. **Verdict**:
   Provide an explicit verdict: **APPROVE** or **REQUEST_CHANGES**.

### Deliverable:
Write `review.md` and a concise `handoff.md` in your working directory. Notify parent with verdict and report path.
