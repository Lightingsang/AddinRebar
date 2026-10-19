# BRIEFING — 2026-10-17T14:31:00Z

## Mission
Migrate HPKata.UI from MaterialDesignThemes to MahApps.Metro 2.4.11 across all 13 dialogs, upgrade Revit element geometry extraction with safe picker fallback, and provide 100% STA test coverage with StaticResource parity.

## 🔒 My Identity
- Archetype: orchestrator
- Roles: orchestrator, user_liaison, human_reporter, successor
- Working directory: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_19\
- Original parent: Sentinel
- Original parent conversation ID: 7e24d92c-711c-48ce-bbb8-c38e9b46a4f5

## 🔒 My Workflow
- **Pattern**: Project
- **Scope document**: g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md
1. **Decompose**: Survey full scope with 3 Explorers, create PROJECT.md with architecture, feature inventory, milestones, and interface contracts.
2. **Dispatch & Execute**:
   - Milestone M1: Package Migration & Theme Infrastructure (HPKata.UI, HPKata.Revit, HPKata.AutoCad)
   - Milestone M2: 13 MetroWindow Views Migration & DialogResult Safety in HPKata.UI/Views/
   - Milestone M3: Revit Element Geometry Extraction & Picker Fallback in HPKata.Revit
   - Milestone M4: STA Unit Tests & StaticResource Parity in HPKata.UI.Tests
   - Milestone M5: Final Milestone: Full Solution Build & 100% Test Pass (Tiers 1-4 + Tier 5 Hardening)
3. **On failure** (in this order):
   - Retry: nudge stuck agent or re-send task
   - Replace: spawn fresh agent with partial progress
   - Skip: proceed without (only if non-critical)
   - Redistribute: split stuck agent's remaining work
   - Redesign: re-partition decomposition
   - Escalate: report to parent (last resort)
4. **Succession**: At 16 spawns, write handoff.md, kill timers, spawn successor.
- **Work items**:
  1. Survey & Feature Inventory [done]
  2. Milestone M1: Package & Theme Infrastructure [done]
  3. Milestone M2: 13 Views Migration [done]
  4. Milestone M3: Revit Geometry Extraction & Fallback [done]
  5. Milestone M4: STA Unit Tests & Parity [done]
  6. Milestone M5: Final E2E Build, Verification & Audit [done]
- **Current phase**: 3 (Complete — Victory)
- **Current focus**: Final Report to Sentinel parent 7e24d92c-711c-48ce-bbb8-c38e9b46a4f5

## 🔒 Key Constraints
- NEVER write, modify, or create source code files directly.
- NEVER run build/test commands yourself — require workers to do so.
- NEVER investigate or explore the problem at the code level — dispatch Explorers for technical investigation.
- You MAY use file-editing tools ONLY for metadata/state files (.md) in your .agents/teamwork/ folder.
- DO NOT CHEAT: zero tolerance for fake passes, hardcoded mocks, or circumventing tests.
- C# files <= 300 lines (Rule C2), methods <= 50 lines (Rule M3).
- 0 warnings (`TreatWarningsAsErrors=true`), 0 dead code, 0 unannotated catch blocks.
- Build/test commands: `dotnet build HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1` and `dotnet test HPKata/HPKata.slnx -c Debug --disable-build-servers -m:1`.

## Current Parent
- Conversation ID: 7e24d92c-711c-48ce-bbb8-c38e9b46a4f5
- Updated: 2026-10-17T14:31:00Z

## Key Decisions Made
- Project pattern selected with 5 milestones (M1: Theme Infra, M2: Views, M3: Revit Geometry, M4: STA Tests, M5: Final Verification).
- Starting Phase 0 Survey with 3 parallel explorers to inspect HPKata packages, Theme.xaml, Views, Commands, and Tests.

## Team Roster
| Agent | Type | Work Item | Status | Conv ID |
|-------|------|-----------|--------|---------|
| explorer_survey_1 | teamwork_preview_explorer | Survey packages, theme & MaterialDesign | completed | 4ac33edd-a01a-428a-92ec-e2ee69df6ee1 |
| explorer_survey_2 | teamwork_preview_explorer | Survey 13 WPF views & XAML keys | completed | cfb27285-92c9-44a6-a065-ef31d80deb3d |
| explorer_survey_3 | teamwork_preview_explorer | Survey Revit commands, geometry & tests | completed | 4d7e4d40-0880-4eb4-a459-a4d3434967b2 |
| worker_m1_1 | teamwork_preview_worker | Milestone M1 implementation | completed | ecc9f454-8bf5-4263-b48c-975ec9354797 |
| reviewer_m1_1 | teamwork_preview_reviewer | M1 independent review | completed | 199cbebd-f5f0-469e-a7f9-3a00fd159047 |
| reviewer_m1_2 | teamwork_preview_reviewer | M1 adversarial review | completed | 69bda23b-8bad-4574-9cd0-89a28cf5195e |
| challenger_m1_1 | teamwork_preview_challenger | M1 empirical verification | completed | 5021ff63-7bae-4e50-988a-fb926e71cb19 |
| challenger_m1_2 | teamwork_preview_challenger | M1 stress test | completed | c1b19672-0453-4938-9a60-6cf98372a830 |
| auditor_m1_1 | teamwork_preview_auditor | M1 forensic integrity audit | completed | 34f12b7e-f477-4031-8857-5e99c907489f |
| worker_m2_1 | teamwork_preview_worker | Milestone M2 13 views migration | completed | 81677f19-c095-4c99-8fc9-3051fd553e70 |
| reviewer_m2_1 | teamwork_preview_reviewer | M2 independent review | completed | 2e0ddc45-ba94-41a4-b234-e7d96e0c6326 |
| reviewer_m2_2 | teamwork_preview_reviewer | M2 adversarial review | completed | a66e4b06-d663-44b5-93f1-236afcf74f4f |
| challenger_m2_1 | teamwork_preview_challenger | M2 STA instantiation verification | completed | 2aae9305-8d74-4db1-b8f4-2e73d3000763 |
| challenger_m2_2 | teamwork_preview_challenger | M2 modeless close stress test | completed | fb904bfa-33a6-4517-89c0-7fda08ea25f5 |
| auditor_m2_1 | teamwork_preview_auditor | M2 forensic integrity audit | completed | 98ff1394-7b68-415a-81af-ec2fa2651f96 |
| worker_m3_1 | teamwork_preview_worker | Milestone M3 Revit geometry & fallback | completed | b7d52647-4679-44b0-a6a3-42b7f7604791 |
| reviewer_m3_1 | teamwork_preview_reviewer | M3 independent review | completed | daba6851-fbfd-43ed-b193-d4ef54ba2e4f |
| reviewer_m3_2 | teamwork_preview_reviewer | M3 adversarial review | completed | f3d92243-7e05-4416-9c3a-1a52c2d73b26 |
| challenger_m3_1 | teamwork_preview_challenger | M3 empirical verification | completed | ba2b2b38-857c-4bd7-80ce-c71e3d9c0777 |
| challenger_m3_2 | teamwork_preview_challenger | M3 stress test | completed | af457f80-c3c3-4a31-9ffd-93f88fd11c89 |
| auditor_m3_1 | teamwork_preview_auditor | M3 forensic integrity audit | completed | 70ef33cf-d8a4-4b04-88af-9e63e46d4a8d |
| worker_m4_1 | teamwork_preview_worker | Milestone M4 STA tests & parity | completed | 098c0d8e-326b-4ce5-bcad-9dd3d750216f |
| reviewer_m4_1 | teamwork_preview_reviewer | M4 independent review | completed | 05791fbf-e2e9-4975-908e-bacffbc9f920 |
| reviewer_m4_2 | teamwork_preview_reviewer | M4 adversarial review | completed | e58b3b02-ff63-490f-af07-f9deb666e371 |
| challenger_m4_1 | teamwork_preview_challenger | M4 empirical verification | completed | 9a0ce046-a1ec-40a2-ac1c-3b3e46d7e85d |
| challenger_m4_2 | teamwork_preview_challenger | M4 stress test | completed | a5e9c00b-92c6-4ab0-b040-58f99909e8af |
| auditor_m4_1 | teamwork_preview_auditor | M4 forensic integrity audit | completed | ef38405d-e09a-4685-82ca-394a1b2d2489 |
| challenger_m5_1 | teamwork_preview_challenger | M5 full solution challenge | completed | e64f3e04-d567-4950-8092-9f6729b10d9f |
| reviewer_m5_1 | teamwork_preview_reviewer | M5 architectural review | completed | b0cd7590-8381-4d6c-9de1-5d700eaf0208 |
| auditor_m5_1 | teamwork_preview_auditor | M5 victory forensic audit | completed | 144fc7cc-82d0-40ad-ade1-9042df1d1e50 |

## Succession Status
- Succession required: no
- Spawn count: 30 / 128
- Pending subagents: none
- Predecessor: none
- Successor: none

## Active Timers
- Heartbeat cron: 6ca2dcc3-e478-4dfe-bcd3-44e77f9ffa52/task-329
- Safety timer: none

## Artifact Index
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_19\DISPATCH.md — Task assignment
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\ORIGINAL_REQUEST.md — Authoritative request
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\.agents\teamwork\orchestrator_19\progress.md — Liveness & status tracking
- g:\09-PROJECT AI\01_Revit\02_CshapRevit\01_AddinRebar\PROJECT.md — Global project plan
